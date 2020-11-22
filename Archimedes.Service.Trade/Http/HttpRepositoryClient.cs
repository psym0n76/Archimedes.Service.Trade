using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Trade.Http
{
    public class HttpRepositoryClient : IHttpRepositoryClient
    {

        private readonly ILogger<HttpRepositoryClient> _logger;
        private readonly HttpClient _client;

        public HttpRepositoryClient(IOptions<Config> config, ILogger<HttpRepositoryClient> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task<List<PriceLevelDto>> GetPriceLevelsByMarketByFromDate(string market, DateTime fromDate)
        {
            var response =
                await _client.GetAsync($"price-level/byMarket_byFromdate?market={market}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var priceLevels = await response.Content.ReadAsAsync<IEnumerable<PriceLevelDto>>();

            return priceLevels.ToList();
        }

        public async Task<List<PriceLevelDto>> GetPriceLevelsByMarketByGranularityByFromDate(string market,
            string granularity, DateTime fromDate)
        {
            var response =
                await _client.GetAsync(
                    $"price-level/byMarket_byFromdate?market={market}&granularity={granularity}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var priceLevels = await response.Content.ReadAsAsync<IEnumerable<PriceLevelDto>>();

            return priceLevels.ToList();
        }

        public async Task<List<CandleDto>> GetCandlesByMarketByFromDate(string market, DateTime fromDate)
        {
            var response = await _client.GetAsync($"candle/byMarket_byFromdate?market={market}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var candles = await response.Content.ReadAsAsync<IEnumerable<CandleDto>>();

            return candles.ToList();
        }

        public async Task<List<PriceDto>> GetPricesByMarketByFromDate(string market, string granularity, DateTime fromDate)
        {
            var response =
                await _client.GetAsync(
                    $"price/byMarket_byFromdate?market={market}&granularity={granularity}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var priceLevels = await response.Content.ReadAsAsync<IEnumerable<PriceDto>>();

            return priceLevels.ToList();
        }

        public async Task UpdatePriceLevel(PriceLevelDto priceLevel)
        {
            var payload = new JsonContent(priceLevel);

            var response = await _client.PutAsync("price-level", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"POST Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return;
            }

            _logger.LogInformation($"Added Trade {priceLevel}");
        }

        public async Task AddTrade(TradeDto trade)
        {
            var payload = new JsonContent(trade);

            var response = await _client.PostAsync("trade", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"POST Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return;
            }

            _logger.LogInformation($"Added Trade {trade}");
        }
    }
}