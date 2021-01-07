using System;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class LastPriceLoader : ILastPriceLoader
    {

        private readonly IHttpPriceRepository _lastPrice;
        private readonly ILogger<LastPriceLoader> _logger;

        public LastPriceLoader(IHttpPriceRepository lastPrice, ILogger<LastPriceLoader> logger)
        {
            _lastPrice = lastPrice;
            _logger = logger;
        }

        public async Task<PriceDto> LoadAsync(string market)
        {
            try
            {
                return await LoadLastPrice(market);
            }
            catch (Exception a)
            {
                _logger.LogError($"Error returned from LastPrice repository \n{market} ErrorMessage: {a.Message} \nStackTrace: {a.StackTrace}");
                return new PriceDto();
            }
        }

        private async Task<PriceDto> LoadLastPrice(string market)
        {
            var price = await _lastPrice.GetLastPriceByMarket(market);

            if (price.Ask == 0m && price.Bid == 0m)
            {
                _logger.LogWarning($"LastPrice missing {market} Bid: {price.Bid} Ask: {price.Ask}");
            }
            
            return price;
        }
    }
}