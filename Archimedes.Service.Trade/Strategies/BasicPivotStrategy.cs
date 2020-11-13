using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;
using Archimedes.Service.Trade.Http;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Archimedes.Service.Price
{
    public class BasicPivotStrategy : IBasicPivotStrategy
    {
        private decimal _lastBidPrice = 0m;
        private decimal _lastAskPrice = 0m;

        private readonly IHttpRepositoryClient _httpRepository;
        private readonly ILogger<BasicPivotStrategy> _logger;
        private readonly IPriceSubscriber _priceSubscriber;
        private readonly IPriceLevelSubscriber _priceLevelSubscriber;
        private readonly ICandleSubscriber _candleSubscriber;

        private readonly List<PriceLevel> _priceLevels = new List<PriceLevel>();
        private readonly List<CandleDto> _candles = new List<CandleDto>();
        private readonly List<Transaction> _transactions = new List<Transaction>();

        private const string Market = "GBP/USD";
        private const string Granularity = "15Min";
        private readonly IMapper _mapper;

        public BasicPivotStrategy(ILogger<BasicPivotStrategy> log, IHttpRepositoryClient httpRepository, IMapper mapper,
            IPriceSubscriber priceSubscriber, ICandleSubscriber candleSubscriber,
            IPriceLevelSubscriber priceLevelSubscriber)
        {
            _logger = log;
            _httpRepository = httpRepository;
            _mapper = mapper;

            _priceSubscriber = priceSubscriber;
            _candleSubscriber = candleSubscriber;
            _priceLevelSubscriber = priceLevelSubscriber;

            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler;
            _candleSubscriber.CandleMessageEventHandler += CandleSubscriber_CandleMessageEventHandler;
            _priceSubscriber.PriceMessageEventHandler += PriceSubscriber_PriceMessageEventHandler;
        }

        public void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var priceLevel = JsonConvert.DeserializeObject<List<PriceLevel>>(e.Message);
            UpdatePriceLevel(priceLevel);
        }

        public void CandleSubscriber_CandleMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var candleDto = JsonConvert.DeserializeObject<List<CandleDto>>(e.Message);
            UpdateCandles(candleDto);
        }

        public void PriceSubscriber_PriceMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var price = JsonConvert.DeserializeObject<PriceDto>(e.Message);
            UpdateTrade(price);
            UpdateTransactionPriceTargets(price);
        }

        public void UpdateTransactionPriceTargets(PriceDto price)
        {
            foreach (var target in _transactions.SelectMany(transaction => transaction.ProfitTargets))
            {
                target.UpdateTrade(price);
            }

            foreach (var target in _transactions.SelectMany(transaction => transaction.StopTargets))
            {
                target.UpdateTrade(price);
            }
        }


        public async Task Consume(CancellationToken cancellationToken)
        {
            var priceLevels =
                await _httpRepository.GetPriceLevelsByMarketByGranularityByFromDate(Market, Granularity,
                    DateTime.Today.AddDays(-100));
            _priceLevels.AddRange(MapLevels(priceLevels));

            var candles = await _httpRepository.GetCandlesByMarketByFromDate(Market, DateTime.Today.AddDays(-100));
            _candles.AddRange(candles);
        }

        public void UpdatePriceLevel(List<PriceLevel> priceLevel)
        {
            foreach (var level in priceLevel.Where(level =>
                !_priceLevels.Select(a => a.TimeStamp).Contains(level.TimeStamp)))
            {
                _priceLevels.Add(level);
            }
        }

        public void UpdateCandles(List<CandleDto> candleDto)
        {
            foreach (var level in candleDto)
            {
                if (!_candles.Select(a => a.TimeStamp).Contains(level.TimeStamp))
                {
                    _candles.Add(level);
                }
            }
        }

        public void UpdateTrade(PriceDto price)
        {
            //check if range has been broken 
            // this could be a slow query
            foreach (var priceLevel in _priceLevels.Where(a =>
                a.TimeStamp < price.TimeStamp && a.TimeStamp > price.TimeStamp.AddDays(-1)))
            {
                if (priceLevel.TradeType == "BUY")
                {
                    if (price.Ask < priceLevel.AskPrice && _lastAskPrice > priceLevel.AskPrice)
                    {
                        //trade has passed the entry zone
                        if (!priceLevel.LevelBroken)
                        {
                            var trade = new Transaction(5, 3, 1.32m, 3, "", 100, priceLevel.TradeType, null);
                            priceLevel.LevelBroken = true;
                            priceLevel.LevelBrokenDate = DateTime.Now;
                            priceLevel.Trades++;
                            _transactions.Add(trade);
                            PostTrade(trade);
                        }
                    }

                    _lastAskPrice = priceLevel.AskPrice;
                }

                if (priceLevel.TradeType == "SELL")
                {
                    if (price.Bid > priceLevel.BidPrice && _lastBidPrice < priceLevel.BidPrice)
                    {
                        if (!priceLevel.LevelBroken)
                        {
                            var trade = new Transaction(5, 3, 1.32m, 3, "", 100, priceLevel.TradeType, null);
                            priceLevel.LevelBroken = true;
                            priceLevel.LevelBrokenDate = DateTime.Now;
                            priceLevel.Trades++;
                            _transactions.Add(trade);
                            PostTrade(trade);
                        }
                    }

                    _lastBidPrice = priceLevel.BidPrice;
                }
            }
        }

        public void PostTrade(Transaction transaction)
        {
            foreach (var profitTarget in transaction.ProfitTargets)
            {
                var tradeDto = new TradeDto()
                {
                    Market = transaction.PriceLevel.Market,
                    BuySell = transaction.BuySell,
                    EntryPrice = profitTarget.EntryPrice,
                    TargetPrice = profitTarget.TargetPrice,
                    ClosePrice = transaction.StopTargets.First().TargetPrice,
                    Strategy = transaction.RiskRewardProfile,
                    Success = false,
                    Timestamp = DateTime.Now
                };

                _httpRepository.AddTrade(tradeDto);
            }
        }

        private IEnumerable<PriceLevel> MapLevels(IEnumerable<PriceLevelDto> levels)
        {
            return _mapper.Map<IEnumerable<PriceLevel>>(levels);
        }
    }
}