using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class PriceTradeExecutor : IPriceTradeExecutor
    {
        private readonly ILogger<PriceTradeExecutor> _logger;
        private readonly object _locker = new();
        private readonly ICacheManager _cache;
        private const string CacheName = "price-levels";
        private const string LastPriceCache = "price";
        private readonly IPriceTradePublisher _publisher;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public PriceTradeExecutor(ILogger<PriceTradeExecutor> logger, ICacheManager cache, IPriceTradePublisher publisher)
        {
            _logger = logger;
            _cache = cache;
            _publisher = publisher;
        }

        public void ExecuteLocked(PriceDto price, decimal tolerance)
        {
            lock (_locker)
            {
                try
                {
                    _logId = _batchLog.Start();
                    Execute(price, tolerance);
                    _logger.LogInformation(_batchLog.Print(_logId));
                }
                catch (Exception e)
                {
                    _logger.LogError(_batchLog.Print(_logId,
                        $"Error returned from ExecuteLocked Price: {price} Tolerance: {tolerance}", e));
                }
            }
        }

        public async void Execute(PriceDto price, decimal tolerance)
        {
            if (await ValidateLastPrice(price))
            {
                _batchLog.Update(_logId, $"Validating LastPrice Bid: {price.Bid} Ask: {price.Ask} Missing from Cache");
                return;
            }

            await ValidatePriceAgainstPriceLevel(price, tolerance);

            await _publisher.UpdateLastPriceCache(price);
        }

        private async Task<bool> ValidateLastPrice(PriceDto price)
        {
            var lastPrice = await _cache.GetAsync<PriceDto>(LastPriceCache);

            _batchLog.Update(_logId,
                $"Validating LastPrice currently set to Bid: {lastPrice.Bid} Ask: {lastPrice.Ask} {lastPrice.TimeStamp}");

            if (lastPrice.Ask != 0 && lastPrice.Bid != 0) return false;

            await _publisher.UpdateLastPriceCache(price);

            return true;
        }

        private async Task ValidatePriceAgainstPriceLevel(PriceDto price, decimal tolerance)
        {
            _batchLog.Update(_logId, $"Bid: {price.Bid} Ask: {price.Ask} {price.TimeStamp}");

            var lastPrice = await _cache.GetAsync<PriceDto>(LastPriceCache);
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(CacheName);

            _batchLog.Update(_logId,
                $"Validate Price against {cachePriceLevels.Where(WithinRangeAndActiveLevelUnbroken()).Count()} PriceLevel(s)");

            PrintPriceLevels(cachePriceLevels);

            foreach (var priceLevel in cachePriceLevels.Where(WithinRangeAndActiveLevelUnbroken()))
            {
                if (priceLevel.BuySell == "BUY")
                {
                    if (!ValidateLastPriceCrossingBuyLevel(price, lastPrice.Ask, priceLevel, tolerance)) continue;

                    await _publisher.UpdateSubscribers(priceLevel);
                    break;
                }

                if (priceLevel.BuySell == "SELL")
                {
                    if (!ValidateLastPriceCrossingSellLevel(price, lastPrice.Bid, priceLevel, tolerance)) continue;

                    await _publisher.UpdateSubscribers(priceLevel);
                    break;
                }
            }

            await _publisher.UpdatePriceLevelCache(cachePriceLevels);
        }


        private static Func<PriceLevelDto, bool> WithinRangeAndActiveLevelUnbroken()
        {
            return level => level.OutsideRange == false && level.Active && level.LevelBroken == false;
        }



        //todo extension method or class
        private void PrintPriceLevels(List<PriceLevelDto> priceLevels)
        {
            foreach (var priceLevel in priceLevels.Where(level => level.OutsideRange == false && level.Active))
            {
                PriceLevelLog(priceLevel);
            }
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

            if (priceLevel.BuySell == "SELL")
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



        public bool ValidateLastPriceCrossingSellLevel(PriceDto price, decimal lastBidPrice, PriceLevelDto priceLevel,
            decimal tolerance)
        {
            if (price.Bid + tolerance <= priceLevel.BidPrice || lastBidPrice >= priceLevel.BidPrice) return false;

            _batchLog.Update(_logId,
                $"BidPrice {price.Bid} + {tolerance} has crossed SELL PriceLevel {priceLevel.BidPrice} {priceLevel.TimeStamp}");
            return true;
        }

        public bool ValidateLastPriceCrossingBuyLevel(PriceDto price, decimal lastAskPrice, PriceLevelDto priceLevel,
            decimal tolerance)
        {
            if (price.Ask - tolerance >= priceLevel.AskPrice || lastAskPrice <= priceLevel.AskPrice) return false;

            _batchLog.Update(_logId,
                $"AskPrice {price.Ask} - {tolerance} has crossed BUY PriceLevel {priceLevel.AskPrice} {priceLevel.TimeStamp}");
            return true;
        }
    }
}