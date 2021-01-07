using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Trade.Http
{
    public class HttpPriceRepository : IHttpPriceRepository
    {

        private readonly ILogger<HttpPriceRepository> _logger;
        private readonly HttpClient _client;

        public HttpPriceRepository(IOptions<Config> config, ILogger<HttpPriceRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }


        public async Task<List<PriceDto>> GetPricesByMarketByFromDate(string market, string granularity, DateTime fromDate)
        {
            var response =
                await _client.GetAsync(
                    $"price/byMarket_byFromdate?market={market}&granularity={granularity}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = response.Content.ReadAsAsync<PriceDto>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  {errorResponse} from {response.RequestMessage.RequestUri}");
                return new List<PriceDto>();
            }

            return await response.Content.ReadAsAsync<List<PriceDto>>();
        }

        public async Task<PriceDto> GetLastPriceByMarket(string market)
        {
            var response =
                await _client.GetAsync(
                    $"price/byLastPrice_byMarket?market={market}");

            if (!response.IsSuccessStatusCode)
            {

                var errorResponse = response.Content.ReadAsAsync<PriceDto>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  {errorResponse} from {response.RequestMessage.RequestUri}");
                
                return new PriceDto();
            }

            return await response.Content.ReadAsAsync<PriceDto>();
        }
    }
}