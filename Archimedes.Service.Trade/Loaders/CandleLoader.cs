using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class CandleLoader : ICandleLoader
    {

        private readonly IHttpCandleRepository _candle;
        private readonly ILogger<CandleLoader> _logger;

        public CandleLoader(IHttpCandleRepository candle, ILogger<CandleLoader> logger)
        {
            _candle = candle;
            _logger = logger;
        }

        public async Task<List<CandleDto>> LoadAsync(string market, string granularity, DateTime startDate)
        {
            try
            {
                return await LoadCandles(market, granularity, startDate);
            }
            catch (Exception a)
            {
                _logger.LogError($"Error returned from Candle repository {market} {a.Message} {a.StackTrace}");
                return new List<CandleDto>();
            }
        }

        public async Task<List<CandleDto>> LoadPreviousFiveCandlesAsync(string market, string granularity,
            DateTime messageStartDate)
        {
            try
            {
                var interval = granularity.ExtractTimeInterval();
                
                var candles =  await LoadCandles(market, granularity, messageStartDate.AddMinutes(-5 * interval));

                return candles;
            }
            catch (Exception a)
            {
                _logger.LogError($"Error returned from Candle Table {market} {a.Message} {a.StackTrace}");
                return new List<CandleDto>();
            }
        }

        private async Task<List<CandleDto>> LoadCandles(string market, string granularity, DateTime startDate)
        {
            var candles = await _candle.GetCandlesByGranularityMarketByDate(market, granularity, startDate, DateTime.Now.AddDays(1));

            if (!candles.Any())
            {
                _logger.LogError($"No Candles returned from Table for {market} {granularity} from {startDate}");
            }

            if (candles.Count() < 3)
            {
                _logger.LogError($"Only {candles.Count} Candle(s) returned from Table for {market} {granularity} from {startDate} - should be 5");
            }

            return candles;
        }
    }
}