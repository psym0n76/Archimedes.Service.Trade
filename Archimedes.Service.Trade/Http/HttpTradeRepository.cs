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
    public class HttpTradeRepository : IHttpTradeRepository
    {
        private readonly ILogger<HttpTradeRepository> _logger;
        private readonly HttpClient _client;

        public HttpTradeRepository(IOptions<Config> config, ILogger<HttpTradeRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task AddTrades(List<TradeDto> trade)
        {
            var payload = new JsonContent(trade);

            var response = await _client.PostAsync("trade", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return;
            }

            _logger.LogInformation($"Added Trade {trade[0].Strategy} {trade[0].BuySell} {trade[0].TargetPrice}");
        }

        public async Task UpdateTrade(TradeDto trade)
        {
            var payload = new JsonContent(trade);

            var response = await _client.PutAsync("trade", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"PUT Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return;
            }

            _logger.LogInformation($"Updated Trade {trade.Strategy} {trade.BuySell} {trade.EntryPrice}");
        }
     }
}