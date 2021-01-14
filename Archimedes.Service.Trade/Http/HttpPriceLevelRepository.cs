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
    public class HttpPriceLevelRepository : IHttpPriceLevelRepository
    {
        private readonly ILogger<HttpPriceLevelRepository> _logger;
        private readonly HttpClient _client;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public HttpPriceLevelRepository(IOptions<Config> config, ILogger<HttpPriceLevelRepository> logger,
            HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task UpdatePriceLevels(List<PriceLevelDto> levels)
        {
            foreach (var level in levels)
            {
                await UpdatePriceLevel(level);
            }
        }

        public async Task UpdatePriceLevel(PriceLevelDto level)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"PUT {nameof(UpdatePriceLevel)} {level.TimeStamp} {level.BuySell} {level.Strategy}");
            
            var payload = new JsonContent(level);

            var response = await _client.PutAsync("price-level", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();
                
                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"PUT FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
            }

            _logger.LogInformation(_batchLog.Print(_logId));
        }

        public async Task<List<PriceLevelDto>> GetPriceLevelsByMarketByFromDate(string market, DateTime fromDate)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetPriceLevelsByMarketByFromDate)} {market} {fromDate}");

            var response =
                await _client.GetAsync($"price-level/byMarket_byFromdate?market={market}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new List<PriceLevelDto>();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<PriceLevelDto>();
            }

            var priceLevels = await response.Content.ReadAsAsync<List<PriceLevelDto>>();
            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {priceLevels.Count} PriceLevel(s)"));
            return priceLevels;
        }

        public async Task<List<PriceLevelDto>> GetPriceLevelsByCurrentAndPreviousDay(string market, string granularity)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetPriceLevelsByCurrentAndPreviousDay)} {market} {granularity}");

            var response =
                await _client.GetAsync(
                    $"price-level/byMarket_byGranularity_byCurrentDay?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new List<PriceLevelDto>();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<PriceLevelDto>();
            }

            var priceLevels = await response.Content.ReadAsAsync<List<PriceLevelDto>>();
            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {priceLevels.Count} PriceLevel(s)"));
            return priceLevels;
        }
    }
}