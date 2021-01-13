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
    public class HttpPriceRepository : IHttpPriceRepository
    {
        private readonly ILogger<HttpPriceRepository> _logger;
        private readonly HttpClient _client;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public HttpPriceRepository(IOptions<Config> config, ILogger<HttpPriceRepository> logger, HttpClient client)
        {
            client.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = logger;
            _client = client;
        }

        public async Task<List<PriceDto>> GetPricesByMarketByFromDate(string market, string granularity, DateTime fromDate)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetPricesByMarketByFromDate)} {market} {granularity} {fromDate}");

            var response =
                await _client.GetAsync(
                    $"price/byMarket_byFromdate?market={market}&granularity={granularity}&fromDate={fromDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new List<PriceDto>();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<PriceDto>();
            }

            var prices = await response.Content.ReadAsAsync<List<PriceDto>>();
            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {prices.Count} Price(s)"));
            return prices;
        }

        public async Task<PriceDto> GetLastPriceByMarket(string market)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET {nameof(GetPricesByMarketByFromDate)} {market}");
            
            var response =
                await _client.GetAsync(
                    $"price/byLastPrice_byMarket?market={market}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(_batchLog.Print(_logId, $"GET FAILED: {errorResponse}"));
                    return new PriceDto();
                }

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET FAILED: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new PriceDto();
            }

            var price = await response.Content.ReadAsAsync<PriceDto>();
            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {price.Bid} {price.Ask} {price.TimeStamp} Price(s)"));
            return price;
        }
    }
}