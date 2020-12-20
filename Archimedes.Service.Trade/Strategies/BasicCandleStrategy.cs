using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Enums;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class BasicCandleStrategy : IBasicCandleStrategy
    {
        public event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;
        private readonly ILogger<BasicCandleStrategy> _logger;
        private readonly ICandleSubscriber _candleSubscriber;
        private readonly ICandleLoader _candleLoader;
        private readonly ICacheManager _cache;
        private const string PriceLevelCache = "price-levels";

        private readonly ITradeProfileFactory _tradeProfileFactory;

        private readonly List<CandleDto> _candles = new();
        private readonly List<Candle> _loadedCandles = new();
        private readonly BatchLog _batchLog = new();
        private string _logId;
        
        private string _tradeProfile;

        public BasicCandleStrategy(ICandleSubscriber candleSubscriber, ILogger<BasicCandleStrategy> logger, ICandleLoader candleLoader, ICacheManager cache, ITradeProfileFactory tradeProfileFactory)
        {
            _logger = logger;
            _cache = cache;
            _candleSubscriber = candleSubscriber;
            _candleLoader = candleLoader;
            _tradeProfileFactory = tradeProfileFactory;
        }

        public void Consume(string tradeProfile, CancellationToken token)
        {
            _candleSubscriber.CandleMessageEventHandler += CandleSubscriber_CandleMessageEventHandler;
            _logger.LogInformation("Consume Candles");
            _tradeProfile = tradeProfile;
            _candleSubscriber.Consume(token);
        }

        private void CandleSubscriber_CandleMessageEventHandler(object sender, CandleMessageHandlerEventArgs e)
        {

            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"Candle Update Received {e.Candle}");
            
            AddCandle(e.Candle);

            if (LoadCandles()) // missing granularity
            {
                ValidatePrice();
            }

            _logger.LogInformation(_batchLog.Print(_logId));
            
        }

        private bool LoadCandles()
        {
            try
            {
                _batchLog.Update(_logId, $"Candle load started {_candles.Count}");
                var loadedCandles = _candleLoader.Load(_candles); // its only loading on candle but needs history

                if (loadedCandles.Any())
                {
                    _batchLog.Update(_logId, $"Candles loaded {loadedCandles.Count}");
                    _loadedCandles.Clear();
                    _batchLog.Update(_logId, $"Candles cleared from cache");
                    _loadedCandles.AddRange(loadedCandles);
                    _batchLog.Update(_logId, $"Candles added to cache  {loadedCandles.Count}");
                }

                else
                {
                    _batchLog.Update(_logId, $"Candles not Found");
                    return false;
                }

                return true;
            }

            catch (Exception exception)
            {
                _logger.LogError($"Error return from Archimedes Library {exception.Message} {exception.StackTrace}");
                return false;
            }
        }

        private async void ValidatePrice()
        {
            await ValidatePriceLevelsOpen(_loadedCandles.Last());
            await ValidatePriceLevelByEngulf(_loadedCandles.Last());
        }


        public void AddCandle(CandleDto candle)
        {
            _batchLog.Update(_logId, $"Attempting to add Candle: {candle.TimeStamp}");
            if (!_candles.Select(a => a.TimeStamp).Contains(candle.TimeStamp))
            {
                _batchLog.Update(_logId, $"Candle added: {candle.TimeStamp}");
                _candles.Add(candle);
            }
            else
            {
                _batchLog.Update(_logId, $"Candle not added: {candle.TimeStamp}");
            }
        }

        private async Task ValidatePriceLevelByEngulf(Candle lastCandle)
        {
            _batchLog.Update(_logId, $"ValidatePriceLevelByEngulf Start {lastCandle.TimeStamp}");
            
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            foreach (var level in cachePriceLevels.Where(level => level.Active))
            {
                ValidateBuyPriceLevel(level, lastCandle);
                ValidateSellPriceLevel(level, lastCandle);
            }

            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
            _logger.LogInformation("ValidatePriceLevelByEngulf End");
        }

        private async Task ValidatePriceLevelsOpen(Candle lastCandle)
        {
            _batchLog.Update(_logId, $"ValidatePriceLevelsOpen Start {lastCandle.TimeStamp}");
            
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            foreach (var level in cachePriceLevels.Where(level => level.Active))
            {
                ProcessEngulfCandle(lastCandle, level);
            }

            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
            _logger.LogInformation("ValidatePriceLevelsOpen End");
        }

        private void ValidateSellPriceLevel(PriceLevelDto level, Candle lastCandle)
        {
            if (level.BuySell == "SELL" && lastCandle.Close.Bid > level.BidPriceRange)
            {
                _batchLog.Update(_logId, $"ValidateSellPriceLevel - VALID {level.TimeStamp}");
                level.Active = false;
            }
        }

        private void ValidateBuyPriceLevel(PriceLevelDto level, Candle lastCandle)
        {
            if (level.BuySell == "BUY" && lastCandle.Close.Ask < level.AskPriceRange)
            {
                _batchLog.Update(_logId, $"ValidateBuyPriceLevel - VALID {level.TimeStamp}");
                level.Active = false;
            }
        }

        private void ProcessEngulfCandle(Candle candle, PriceLevelDto priceLevelDto)
        {
            _batchLog.Update(_logId, "ProcessEngulfCandle");
            
            if (candle.Type() == CandleType.Engulfing && candle.BodyFillRate() > 0.5m &&
                candle.Color() == priceLevelDto.BuySell.Color())
            {
                priceLevelDto.Active = false;
                _batchLog.Update(_logId, "ProcessEngulfCandle - Trade Booked");

                var price = new PriceDto()
                {
                    Bid = candle.Fibonacci382(),
                    Ask = candle.Fibonacci382(),
                    TimeStamp = DateTime.Now,
                    Granularity = "5Min",
                    Market = candle.Market
                };

                BuildTradeEvent(price, priceLevelDto.BuySell);
            }
        }

        private void BuildTradeEvent(PriceDto price, string buySell)
        {
            // move this to Transaction stuff
            _batchLog.Update(_logId, "BuildTradeEvent - TradeBooked");
            var transaction = _tradeProfileFactory.GetTradeGenerationService(_tradeProfile).Generate(price, buySell);

            var eventArgs = new TradeMessageHandlerEventArgs() { Transaction = transaction };

            TradeMessageEventHandler?.Invoke(this, eventArgs);
        }
    }
}   