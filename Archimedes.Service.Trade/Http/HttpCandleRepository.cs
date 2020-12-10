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
    public class HttpCandleRepository:IHttpCandleRepository
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


        public async Task<List<CandleDto>> GetCandlesByMarketByFromDate(string market, DateTime fromDate)
        {
            var response = await _client.GetAsync($"candle/byMarket_byFromdate?market={market}&fromDate={fromDate:yyyy-MM-dd}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var candles = await response.Content.ReadAsAsync<IEnumerable<CandleDto>>();

            return candles.ToList();
        }
    }
}