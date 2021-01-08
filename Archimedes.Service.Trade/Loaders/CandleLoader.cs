using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class CandleLoader : ICandleLoader
    {
        private readonly IHttpCandleRepository _candle;
        private readonly ILogger<CandleLoader> _logger;
        private readonly BatchLog _batchLog = new BatchLog();
        private string _logId;

        public CandleLoader(IHttpCandleRepository candle, ILogger<CandleLoader> logger)
        {
            _candle = candle;
            _logger = logger;
        }

        public async Task<List<CandleDto>> LoadAsync(string market, string granularity, DateTime startDate)
        {
            try
            {
                _logId = _batchLog.Start();
                return await LoadCandles(market, granularity, startDate);
            }
            catch (Exception a)
            {
                 _logger.LogError(_batchLog.Print(_logId,$"Error returned from Candle Repository {market}", a));
                return new List<CandleDto>();
            }
        }
        
        
        public async Task<List<CandleDto>> LoadCandlesPreviousFiveAsync(string market, string granularity,
            DateTime messageStartDate)
        {
            return await LoadAsync(market, granularity, messageStartDate.AddMinutes(-5 * granularity.ExtractTimeInterval()));
        }

        public async Task ValidateRecentCandle(string market, string granularity, int retries)
        {
            _logId = _batchLog.Start();
            var retry = 1;
            var lastCandle = new DateTime();

            while (retry < retries)
            {
                lastCandle = await _candle.GetLastCandleUpdated(market, granularity);

                if (lastCandle > DateTime.Now.AddHours(-1))
                {
                    _logger.LogInformation(_batchLog.Print(_logId, $"Validated recent Candle {lastCandle} OK"));
                    return;
                }

                _batchLog.Update(_logId,$"ValidateRecentCandle - waiting for Candle to update {market} {granularity} {lastCandle} - Retry {retry} from {retries} waiting 2secs");
                Thread.Sleep(2000);
                retry++;
            }
            
            _logger.LogError($"ValidateRecentCandle - Out of Date Candle {market} {granularity} {lastCandle}");
        }


        private async Task<List<CandleDto>> LoadCandles(string market, string granularity, DateTime startDate)
        {
            var candles = await _candle.GetCandlesByGranularityMarketByDate(market, granularity, startDate, DateTime.Now.AddDays(1));
            
            if (candles.Count() < 3)
            {
                _logger.LogWarning($"Only {candles.Count} Candle(s) returned from Repository for {market} {granularity} from {startDate} - should be 5");
            }

            return candles;
        }
    }
}