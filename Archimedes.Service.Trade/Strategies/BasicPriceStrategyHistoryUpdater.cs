﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class BasicPriceStrategyHistoryUpdater : IBasicPriceStrategyHistoryUpdater
    {
        private readonly ILogger<BasicPriceStrategyHistoryUpdater> _logger;

        private readonly BatchLog _batchLog = new();
        private string _logId;
        private readonly ICandleLoader _candle;
        private readonly IPriceLevelLoader _priceLevel;
        private readonly IHttpPriceLevelRepository _repository;

        public BasicPriceStrategyHistoryUpdater(ILogger<BasicPriceStrategyHistoryUpdater> logger, ICandleLoader candle,
            IPriceLevelLoader priceLevel, IHttpPriceLevelRepository repository)
        {
            _logger = logger;
            _candle = candle;
            _priceLevel = priceLevel;
            _repository = repository;
        }

        public void UpdateHistory(string market, string granularity)
        {
            _logId = _batchLog.Start();

            try
            {
                UpdateHistoryAsync(market, granularity).GetAwaiter().GetResult();
                _logger.LogInformation(_batchLog.Print(_logId));
            }

            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"Error returned from BasicPriceStrategyHistoryUpdater", e));
            }
        }


        private async Task UpdateHistoryAsync(string market, string granularity)
        {
            
            var priceLevels = await _priceLevel.LoadAsync(market, granularity);

            if (!priceLevels.Any())
            {
                _batchLog.Update(_logId, $"WARNING Missing PriceLevel(s)");
                return;
            }
            
            var minDate = priceLevels.Min(a => a.TimeStamp);

            _batchLog.Update(_logId, $"PriceLevel Range: {minDate} to {priceLevels.Max(a => a.TimeStamp)}");
            _batchLog.Update(_logId, $"{priceLevels.Count} PriceLevel(s) returned from Table");

            var candles = await _candle.LoadAsync(market, "1Min", minDate);

            _batchLog.Update(_logId, $"Candle Range:     {candles.Min(a => a.TimeStamp)} to {candles.Max(a => a.TimeStamp)}");
            _batchLog.Update(_logId, $"{candles.Count} Candle(s) returned from Table");

            ScanPriceLevelsAgainstCandles(priceLevels, candles);

            await _repository.UpdatePriceLevels(priceLevels);

            _batchLog.Update(_logId,$"Unbroken Levels remaining {priceLevels.Count(a => a.LevelBroken == false && a.OutsideRange == false)} / {priceLevels.Count}");
        }

        private void ScanPriceLevelsAgainstCandles(List<PriceLevelDto> priceLevels, List<CandleDto> candles)
        {
            foreach (var level in priceLevels)
            {
                _batchLog.Update(_logId, Message(level));
                IsPriceLevelBroken(candles, level);
            }
        }

        private static string Message(PriceLevelDto level)
        {
            return
                $"PriceLevel {level.Strategy.PadRight(12, ' ')} Bid: {Math.Round(level.BidPrice, 4)} Range: {Math.Round(level.BidPriceRange, 4)} Broken: {level.LevelBroken,-5} OutsideRange: {level.OutsideRange, -6} {level.TimeStamp}";
        }

        private void IsPriceLevelBroken(List<CandleDto> candles, PriceLevelDto level)
        {
            var pivots = int.Parse(level.Strategy.Substring(level.Strategy.Length - 1));

            foreach (var candle in candles)
            {

                if (candle.TimeStamp > level.TimeStamp.AddMinutes(level.Granularity.ExtractTimeInterval() * pivots))
                {
                    if (level.BuySell == "BUY")
                    {
                        BuyLevelBroken(level, candle);
                        BuyOutsideRange(level, candle);
                    }

                    if (level.BuySell == "SELL")
                    {
                        SellLevelBroken(level, candle);
                        SellOutsideRange(level, candle);
                    }
                }

                if (level.LevelBroken && level.OutsideRange)
                {
                    break;
                }
            }
        }

        private void BuyLevelBroken(PriceLevelDto level, CandleDto candle)
        {
            if (level.LevelBroken == false)
            {
                if (candle.AskLow < level.AskPrice)
                {
                    level.LevelBroken = true;
                    level.LevelBrokenDate = candle.TimeStamp;
                    level.LastUpdated = DateTime.Now;
                    _batchLog.Update(_logId, $"LevelBroken: {level.LevelBroken} at {candle.TimeStamp}");
                    _batchLog.Update(_logId, $"{Message(level)}");
                }
            }
        }

        private void SellLevelBroken(PriceLevelDto level, CandleDto candle)
        {
            if (level.LevelBroken == false)
            {
                if (candle.BidHigh > level.BidPrice)
                {
                    level.LevelBroken = true;
                    level.LevelBrokenDate = candle.TimeStamp;
                    level.LastUpdated = DateTime.Now;
                    _batchLog.Update(_logId, $"LevelBroken: {level.LevelBroken} at {candle.TimeStamp}");
                    _batchLog.Update(_logId, $"{Message(level)}");
                }
            }
        }

        private void SellOutsideRange(PriceLevelDto level, CandleDto candle)
        {
            if (level.OutsideRange == false && level.LevelBroken)
            {
                if (candle.BidClose > level.BidPriceRange)
                {
                    level.OutsideRange = true;
                    level.OutsideRangeDate = candle.TimeStamp;
                    level.LastUpdated = DateTime.Now;
                    _batchLog.Update(_logId, $"OutsideRange: {level.OutsideRange} at {candle.TimeStamp}");
                    _batchLog.Update(_logId, $"{Message(level)}");
                }
            }
        }

        private void BuyOutsideRange(PriceLevelDto level, CandleDto candle)
        {
            if (level.OutsideRange == false && level.LevelBroken)
            {
                if (candle.AskClose < level.AskPriceRange)
                {
                    level.OutsideRange = true;
                    level.OutsideRangeDate = candle.TimeStamp;
                    level.LastUpdated = DateTime.Now;
                    _batchLog.Update(_logId, $"OutsideRange: {level.OutsideRange} at {candle.TimeStamp}");
                    _batchLog.Update(_logId, $"{Message(level)}");
                }
            }
        }
    }
}
