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
    public class HttpCandleRepository: IHttpCandleRepository
    {

        private readonly ILogger<HttpCandleRepository> _logger;
        private readonly HttpClient _client;

        public HttpCandleRepository(IOptions<Config> config, ILogger<HttpCandleRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate)
        {
            var response = await _client.GetAsync($"candle/bymarket_bygranularity_fromdate_todate?market={market}&granularity={granularity}&fromdate={startDate}&todate={endDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new List<CandleDto>();
            }

            return await response.Content.ReadAsAsync<List<CandleDto>>();
        }
    }
}