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
    public class HttpCandleRepository: IHttpCandleRepository
    {

        private readonly ILogger<HttpCandleRepository> _logger;
        private readonly HttpClient _client;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public HttpCandleRepository(IOptions<Config> config, ILogger<HttpCandleRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetCandlesByGranularityMarketByDate)} {market} {granularity} {startDate} {endDate}");
            
            var response = await _client.GetAsync($"candle/bymarket_bygranularity_fromdate_todate?market={market}&granularity={granularity}&fromdate={startDate}&todate={endDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new List<CandleDto>();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<CandleDto>();
            }

            var candles = await response.Content.ReadAsAsync<List<CandleDto>>();
            _logger.LogInformation(_batchLog.Print(_logId,$"{candles.Count} Candle(s) returned"));
            return candles;
        }

        public async Task<DateTime> GetLastCandleUpdated(string market, string granularity)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetLastCandleUpdated)} {market} {granularity}");
            
            var response = await _client.GetAsync($"candle/bylastupdated?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new DateTime();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new DateTime();
            }

            var updated =  await response.Content.ReadAsAsync<DateTime>();
            _logger.LogInformation(_batchLog.Print(_logId, $"{updated} returned"));
            return updated;
        }
    }
}