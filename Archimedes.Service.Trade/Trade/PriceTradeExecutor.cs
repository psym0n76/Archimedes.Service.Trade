using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
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
        private readonly IHttpPriceLevelRepository _priceLevel;

        private readonly BatchLog _batchLog = new();
        private string _logId;

        public PriceTradeExecutor(ILogger<PriceTradeExecutor> logger, IHttpPriceLevelRepository priceLevel,
            ICacheManager cache)
        {
            _logger = logger;
            _priceLevel = priceLevel;
            _cache = cache;
        }

        public void ExecuteLocked(PriceDto price, decimal tolerance)
        {
            lock (_locker)
            {
                _logId = _batchLog.Start();
                Execute(price, tolerance);
            }
        }

        public async void Execute(PriceDto price, decimal tolerance)
        {
            if (await ValidateLastPrice(price))
            {
                _batchLog.Update(_logId, $"Validating LastPrice Missing from Cache");
                _logger.LogInformation(_batchLog.Print(_logId));
                return;
            }

            await ValidatePriceAgainstPriceLevel(price, tolerance);
            await UpdateLastPriceCache(price);

            _logger.LogInformation(_batchLog.Print(_logId));
        }

        private async Task<bool> ValidateLastPrice(PriceDto price)
        {
            var lastPrice = await _cache.GetAsync<PriceDto>(LastPriceCache);

            _batchLog.Update(_logId,
                $"Validating LastPrice currently set to Bid: {lastPrice.Bid} Ask: {lastPrice.Ask} {lastPrice.TimeStamp}");

            if (lastPrice.Ask != 0 && lastPrice.Bid != 0) return false;

            await UpdateLastPriceCache(price);

            return true;

        }

        private async Task ValidatePriceAgainstPriceLevel(PriceDto price, decimal tolerance)
        {
            _batchLog.Update(_logId, $"Bid: {price.Bid} Ask: {price.Ask} {price.TimeStamp}");

            var lastPrice = await _cache.GetAsync<PriceDto>(LastPriceCache);
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(CacheName);

            _batchLog.Update(_logId,
                $"Validate Bid: {price.Bid} Ask: {price.Ask} against {cachePriceLevels.Where(WithinRangeAndActiveLevelUnbroken()).Count()} PriceLevel(s)");

            PrintPriceLevels(cachePriceLevels);

            foreach (var priceLevel in cachePriceLevels.Where(WithinRangeAndActiveLevelUnbroken()))
            {
                if (priceLevel.BuySell == "BUY")
                {
                    if (!ValidateLastPriceCrossingBuyLevel(price, lastPrice.Ask, priceLevel, tolerance)) continue;

                    RaiseTradeEvent(priceLevel);
                    await UpdatePriceLevelCache(cachePriceLevels, priceLevel);
                    await UpdatePriceLevelTable(priceLevel);
                    break;
                }

                if (priceLevel.BuySell == "SELL")
                {
                    if (!ValidateLastPriceCrossingSellLevel(price, lastPrice.Bid, priceLevel, tolerance)) continue;

                    RaiseTradeEvent(priceLevel);
                    await UpdatePriceLevelCache(cachePriceLevels, priceLevel);
                    await UpdatePriceLevelTable(priceLevel);
                    break;
                }
            }
        }

        private static Func<PriceLevelDto, bool> WithinRangeAndActiveLevelUnbroken()
        {
            return level => level.OutsideRange == false && level.Active && level.LevelBroken == false;
        }

        private async Task UpdatePriceLevelTable(PriceLevelDto priceLevel)
        {
            _batchLog.Update(_logId,
                $"Update PriceLevel Table {priceLevel.Strategy} {priceLevel.TimeStamp}" );
            await _priceLevel.UpdatePriceLevel(priceLevel);
        }

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
                $"Active: {priceLevel.Active} Broken: {priceLevel.LevelBroken, -6} {priceLevel.LevelBrokenDate} Outside: {priceLevel.OutsideRange,-6} Timestamp: {priceLevel.TimeStamp} ");
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

        private async Task UpdatePriceLevelCache(List<PriceLevelDto> cachePriceLevels, PriceLevelDto priceLevel)
        {
            _batchLog.Update(_logId, $"Updating PriceLevel to Cache");
            PriceLevelLog(priceLevel);
            await _cache.SetAsync(CacheName, cachePriceLevels);
        }

        private async Task UpdateLastPriceCache(PriceDto price)
        {
            _batchLog.Update(_logId, $"Updated LastPrice to Cache ");
            await _cache.SetAsync(LastPriceCache,
                new PriceDto() {Ask = price.Ask, Bid = price.Bid, TimeStamp = price.TimeStamp});
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

        private void RaiseTradeEvent(PriceLevelDto priceLevel)
        {
            priceLevel.LevelBrokenDate = DateTime.Now;
            priceLevel.LevelsBroken++;
            priceLevel.LevelBroken = true;

            _batchLog.Update(_logId,
                $"=================================================================================================================================");
            _batchLog.Update(_logId,
                $"PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED");
            _batchLog.Update(_logId,
                $"=================================================================================================================================");

            _batchLog.Update(_logId, $"Broken by Price and waiting for an Engulfing Candle");
        }
    }
}