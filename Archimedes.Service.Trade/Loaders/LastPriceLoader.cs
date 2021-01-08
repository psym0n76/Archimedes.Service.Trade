using System;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class LastPriceLoader : ILastPriceLoader
    {

        private readonly IHttpPriceRepository _lastPrice;
        private readonly ILogger<LastPriceLoader> _logger;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public LastPriceLoader(IHttpPriceRepository lastPrice, ILogger<LastPriceLoader> logger)
        {
            _lastPrice = lastPrice;
            _logger = logger;
        }

        public async Task<PriceDto> LoadAsync(string market)
        {
            try
            {
                _logId = _batchLog.Start();
                return await LoadLastPrice(market);
            }
            catch (Exception a)
            {
                _logger.LogError(_batchLog.Print(_logId, $"Error returned from LastPriceLoader {market}", a));
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