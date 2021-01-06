using Archimedes.Library.Candles;
using Archimedes.Library.Enums;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Service.Trade.Http;

namespace Archimedes.Service.Trade.Strategies
{
    public class BasicEngulfingCandleStrategy : IEngulfingCandleStrategy
    {
        public event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;
        private readonly ILogger<BasicEngulfingCandleStrategy> _logger;

        private readonly IHttpPriceLevelRepository _priceLevelRepository;
        private readonly ICandleSubscriber _candleSubscriber;
        private readonly ICandleLoader _candleLoader;
        private readonly ICandleHistoryLoader _historyLoader;

        private readonly ICacheManager _cache;
        private const string PriceLevelCache = "price-levels";

        private readonly ITradeProfileFactory _tradeProfileFactory;

        private List<CandleDto> _candles = new();
        private readonly List<Candle> _loadedCandles = new();

        private readonly BatchLog _batchLog = new();
        private string _logId;

        private string _tradeProfile;

        public BasicEngulfingCandleStrategy(ICandleSubscriber candleSubscriber,
            ILogger<BasicEngulfingCandleStrategy> logger,
            ICandleLoader candleLoader, ICacheManager cache, ITradeProfileFactory tradeProfileFactory,
            ICandleHistoryLoader historyLoader, IHttpPriceLevelRepository priceLevelRepository)
        {
            _logger = logger;
            _cache = cache;
            _candleSubscriber = candleSubscriber;
            _candleLoader = candleLoader;
            _tradeProfileFactory = tradeProfileFactory;
            _historyLoader = historyLoader;
            _priceLevelRepository = priceLevelRepository;
        }

        public void Consume(string market, string granularity, string tradeProfile, CancellationToken token)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"Consume Candle(s) [{tradeProfile}]");

            _tradeProfile = tradeProfile;

            _logger.LogInformation(_batchLog.Print(_logId));

            _candleSubscriber.CandleMessageEventHandler += CandleSubscriber_CandleMessageEventHandler;
            _candleSubscriber.Consume(token);
        }

        private void CandleSubscriber_CandleMessageEventHandler(object sender, CandleMessageHandlerEventArgs e)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,
                $"Candle update {e.Message.Market} {e.Message.Interval}{e.Message.TimeFrame} StartDate: {e.Message.StartDate} EndDate: {e.Message.EndDate}");

            try
            {
                LoadCandlesInternal(e.Message.Market, "5Min", e.Message.StartDate);
                LoadCandles();
                ValidatePrice();
                UpdatePriceLevelTable();
            }
            catch (Exception exception)
            {
                _logger.LogError(_batchLog.Print(_logId,
                    $"Error in EngulfCandleStrategy {exception.Message} {exception.StackTrace}"));
            }

            _logger.LogInformation(_batchLog.Print(_logId));
        }

        private  void UpdatePriceLevelTable()
        {
            var cachePriceLevels = _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache).Result;
            _priceLevelRepository.UpdatePriceLevels(cachePriceLevels).ConfigureAwait(false);

            _batchLog.Update(_logId, "Updating PriceLevel Table");
        }

        public void LoadCandlesInternal(string market, string granularity, DateTime messageStartDate)
        {

            var candles = _candleLoader.LoadPreviousFiveCandlesAsync(market, granularity, messageStartDate)
                .Result; // need to wait for this response

            if (!candles.Any())
            {
                _batchLog.Update(_logId, "Load the last 5 Candle(s) FAILED");
                return;
            }

            if (candles.Count < 3)
            {
                _batchLog.Update(_logId, $"Load the last 5 Candle(s) WARNING only {candles.Count} Candle(s) returned");
                return;
            }

            _batchLog.Update(_logId,
                $"Load {candles.Count} History Candle(s) with range {candles.Min(a => a.TimeStamp)} to {candles.Max(a => a.TimeStamp)}");

            foreach (var candle in candles)
            {
                if (!_candles.Select(a => a.TimeStamp).Contains(candle.TimeStamp))
                {
                    _batchLog.Update(_logId,
                        $"ADD History Candle to InternalCandle {candle.Market} {candle.Granularity} {candle.TimeStamp}");
                    _candles.Add(candle);
                }
                else
                {
                    _batchLog.Update(_logId,
                        $"NOT ADDED History Candle to InternalCandle {candle.Market} {candle.Granularity} {candle.TimeStamp}");
                }
            }
        }

        private void LoadCandles()
        {
            try
            {
                _batchLog.Update(_logId, $"Loading InternalCandle into CandleLoader");

                if (!_candles.Any())
                {
                    _batchLog.Update(_logId, "Internal Candles empty - FAILED LoadCandles");
                    return;
                }

                var historyCandles = _historyLoader.Load(_candles);

                if (!historyCandles.Any())
                {
                    _batchLog.Update(_logId, "No Candle(s) returned from CandleHistoryLoader - FAILED LoadCandles");
                    return;
                }

                _batchLog.Update(_logId, $"Candle(s) Loading...CandleLoader Count: {_loadedCandles.Count}");
                _batchLog.Update(_logId, $"Candle(s) Loading...CandleHistoryLoader Count: {historyCandles.Count} COMPLETE");
                _loadedCandles.Clear();
                _loadedCandles.AddRange(historyCandles);
            }

            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"LoadCandle FAILED {e.Message} {e.StackTrace}"));
            }
        }

        private async void ValidatePrice()
        {
            if (!_loadedCandles.Any())
            {
                _batchLog.Update(_logId, "No Candle(s) returned from CandleLoader - FAILED ValidatePrice");
                return;
            }

            await ValidateCandleClosedOutsideOfRange(_loadedCandles.Last());
            await ValidateCandleEngulf(_loadedCandles.Last());
        }

        private async Task ValidateCandleClosedOutsideOfRange(Candle lastCandle)
        {
            _batchLog.Update(_logId, $"Candle Closed Outside of PriceLevel Range...");

            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            foreach (var level in cachePriceLevels.Where(WithinRangeAndActiveLevelBroken()))
            {
                level.CandlesElapsedLevelBroken++;
                BuyCandleClosedOutsideOfRange(level, lastCandle);
                SellCandleClosedOutsideOfRange(level, lastCandle);
            }

            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
            _batchLog.Update(_logId, $"Candle Closed Outside of PriceLevel Range... COMPLETE");
        }

        private async Task ValidateCandleEngulf(Candle lastCandle)
        {
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            _batchLog.Update(_logId,
                $"Identify Engulfing Candle across {cachePriceLevels.Count(WithinRangeAndActiveLevelBroken())} from {cachePriceLevels.Count}");

            foreach (var level in cachePriceLevels.Where(WithinRangeAndActiveLevelBroken()))
            {
                PriceLevelLog(level);
                ProcessEngulfCandle(lastCandle, level);
            }

            _batchLog.Update(_logId, "Update PriceLevel Cache");
            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);

            _batchLog.Update(_logId, "Identify Engulfing Candle... Completed");
        }

        private void PriceLevelLog(PriceLevelDto priceLevel)
        {
            _batchLog.Update(_logId,
                $"{priceLevel.Strategy.PadRight(13, ' ')}" + BidRangeFormat(priceLevel) +
                $"Active: {priceLevel.Active} Broken: {priceLevel.LevelBroken,-6} {priceLevel.LevelBrokenDate} Outside: {priceLevel.OutsideRange,-6} Timestamp: {priceLevel.TimeStamp} ");
        }

        private static string BidRangeFormat(PriceLevelDto priceLevel)
        {
            string result;

            if (priceLevel.Strategy.Contains("HIGH"))
            {
                result = $"Bid: {priceLevel.BidPrice.ToString(CultureInfo.InvariantCulture)?.PadRight(7, ' ')} " +
                         $"Range: {priceLevel.BidPriceRange.ToString(CultureInfo.InvariantCulture)?.PadRight(7, ' ')} ";
            }

            else
            {
                result = $"Ask: {priceLevel.AskPrice.ToString(CultureInfo.InvariantCulture).PadRight(7, ' ')} " +
                         $"Range: {priceLevel.AskPriceRange.ToString(CultureInfo.InvariantCulture).PadRight(7, ' ')} ";
            }

            return result;
        }

        private static Func<PriceLevelDto, bool> WithinRangeAndActiveLevelBroken()
        {
            return level => level.Active && level.OutsideRange == false && level.LevelBroken && level.Trade == false;
        }

        private void SellCandleClosedOutsideOfRange(PriceLevelDto level, Candle lastCandle)
        {
            if (level.BuySell == "SELL" && lastCandle.Close.Bid > level.BidPriceRange)
            {
                _batchLog.Update(_logId,
                    $"OutsideOfRange - SELL and LastCandleClose: {lastCandle.Close.Bid} > PriceLevel: {level.BidPriceRange} on {level.TimeStamp}");
                level.OutsideRange = true;
                level.OutsideRangeDate = lastCandle.TimeStamp;
                level.LastUpdated = DateTime.Now;
            }
        }

        private void BuyCandleClosedOutsideOfRange(PriceLevelDto level, Candle lastCandle)
        {
            if (level.BuySell == "BUY" && lastCandle.Close.Ask < level.AskPriceRange)
            {
                _batchLog.Update(_logId,
                    $"OutsideOfRange - BUY and LastCandleClose: {lastCandle.Close.Ask} < PriceLevel: {level.BidPriceRange} on {level.TimeStamp}");
                level.OutsideRange = true;
                level.OutsideRangeDate = lastCandle.TimeStamp;
                level.LastUpdated = DateTime.Now;
            }
        }

        private void ProcessEngulfCandle(Candle candle, PriceLevelDto level)
        {
            try
            {
                _batchLog.Update(_logId,
                    $"ProcessEngulfCandle on CandleType: {candle.Type()}: BodyFillRate: {candle.BodyFillRate()} Color: {candle.Color()}");

                if (candle.Type() == CandleType.Engulfing && candle.BodyFillRate() > 0.5m &&
                    candle.Color() == level.BuySell.Color())
                {
                    level.Trade = true;
                    level.LastUpdated = DateTime.Now;

                    _batchLog.Update(_logId,
                        $"=======================================================================================================================");
                    _batchLog.Update(_logId,
                        $"ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING ENGULFING");
                    _batchLog.Update(_logId,
                        $"=======================================================================================================================");
                    _batchLog.Update(_logId,
                        $"ProcessEngulfCandle  CONFIRMED ENGULFING- {DateTime.Now} {candle.Market} 5Min EntryPrice = {candle.Fibonacci382()} {candle.Fibonacci382()}");

                    BuildTradeEvent(level.BuySell, candle);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"ProcessEngulfCandle FAILED {e.Message} {e.StackTrace}"));
            }
        }

        private void BuildTradeEvent(string buySell, Candle candle)
        {
            var price = new PriceDto()
            {
                Bid = candle.Fibonacci382(),
                Ask = candle.Fibonacci382(),
                TimeStamp = DateTime.Now,
                Granularity = "5Min",
                Market = candle.Market
            };

            var transaction = _tradeProfileFactory.GetTradeGenerationService(_tradeProfile).Generate(price, buySell);

            var eventArgs = new TradeMessageHandlerEventArgs() {Transaction = transaction};

            TradeMessageEventHandler?.Invoke(this, eventArgs);
        }
    }
}