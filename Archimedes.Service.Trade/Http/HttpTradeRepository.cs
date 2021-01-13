using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Trade.Http
{
    public class HttpTradeRepository : IHttpTradeRepository
    {
        private readonly ILogger<HttpTradeRepository> _logger;
        private readonly HttpClient _client;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public HttpTradeRepository(IOptions<Config> config, ILogger<HttpTradeRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task AddTrades(List<TradeDto> trade)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"POST {nameof(AddTrades)}");
            
            var payload = new JsonContent(trade);

            var response = await _client.PostAsync("trade", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return;
            }

            //todo return id
            _logger.LogInformation(_batchLog.Print(_logId,$"ADDED Trade"));
        }

        public async Task UpdateTrade(TradeDto trade)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"PUT {nameof(UpdateTrade)} {trade.Strategy} {trade.BuySell} {trade.PriceLevelTimestamp}");
            
            var payload = new JsonContent(trade);

            var response = await _client.PutAsync("trade", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"POST FAILED: {errorResponse}"));
                    return;
                }

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"PUT FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return;
            }

            _logger.LogInformation(_batchLog.Print(_logId, $"UPDATED Trade"));
        }

        public async Task UpdateTrades(List<TradeDto> trades)
        {
            foreach (var trade in trades)
            {
                await UpdateTrade(trade);
            }
        }
    }
}