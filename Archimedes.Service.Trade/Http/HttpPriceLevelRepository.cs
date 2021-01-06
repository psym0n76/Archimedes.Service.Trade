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
    public class HttpPriceLevelRepository : IHttpPriceLevelRepository
    {
        private readonly ILogger<HttpPriceLevelRepository> _logger;
        private readonly HttpClient _client;

        public HttpPriceLevelRepository(IOptions<Config> config, ILogger<HttpPriceLevelRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }


        public async Task UpdatePriceLevel(PriceLevelDto level)
        {
            var payload = new JsonContent(level);

            var response = await _client.PutAsync("price-level", payload);

            if (!response.IsSuccessStatusCode)
            {
                if (response.RequestMessage != null)
                    _logger.LogError($"PUT Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
            }
        }

        public async Task UpdatePriceLevels(List<PriceLevelDto> priceLevels)
        {
            foreach (var level in priceLevels)
            {
                await UpdatePriceLevel(level);
            }

            _logger.LogInformation($"UPDATED {priceLevels.Count} PriceLevel(s)");
        }
        

        public async Task<List<PriceLevelDto>> GetPriceLevelsByMarketByFromDate(string market, DateTime fromDate)
        {
            var response =
                await _client.GetAsync($"price-level/byMarket_byFromdate?market={market}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.RequestMessage != null)
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
                    $"price-level/byMarket_byGranularity_byFromdate?market={market}&granularity={granularity}&fromDate={fromDate:yyyy-MM-dd}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.RequestMessage != null)
                    _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var priceLevels = await response.Content.ReadAsAsync<IEnumerable<PriceLevelDto>>();

            return priceLevels.ToList();
        }

        public async Task<List<PriceLevelDto>> GetPriceLevelCurrentAndPreviousDay(string market, string granularity)
        {
            var priceLevelDto =
                await GetPriceLevelsByMarketByGranularityByFromDate(market, granularity,
                    DateTime.Today.PreviousWorkDay().AddDays(-1));

            return priceLevelDto;
        }


    }
}