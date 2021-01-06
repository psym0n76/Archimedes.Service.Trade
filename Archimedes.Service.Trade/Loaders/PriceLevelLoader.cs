using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;
using NLog;

namespace Archimedes.Service.Trade
{
    public class PriceLevelLoader : IPriceLevelLoader
    {

        private readonly IHttpPriceLevelRepository _priceLevel;
        private readonly ILogger<PriceLevelLoader> _logger;

        public PriceLevelLoader(IHttpPriceLevelRepository priceLevel, ILogger<PriceLevelLoader> logger)
        {
            _priceLevel = priceLevel;
            _logger = logger;
        }

        public async Task<List<PriceLevelDto>> LoadAsync(string market, string granularity)
        {
            try
            {
                return await LoadPriceLevels(market, granularity);
            }
            catch (Exception a)
            {
                _logger.LogError($"Error returned from PriceLevel repository {market} {a.Message} {a.StackTrace}");
                throw;
            }
        }

        private async Task<List<PriceLevelDto>> LoadPriceLevels(string market, string granularity)
        {
            var priceLevels = await _priceLevel.GetPriceLevelCurrentAndPreviousDay(market, granularity);

            if (!priceLevels.Any())
            {
                _logger.LogError($"PriceLevel missing {market}");
            }
            
            return priceLevels;
        }
    }
}