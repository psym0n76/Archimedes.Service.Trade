using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpRepositoryClient
    {
        Task<List<PriceLevelDto>> GetPriceLevelsByMarketByFromDate(string market, DateTime fromDate);
        Task<List<PriceLevelDto>> GetPriceLevelsByMarketByGranularityByFromDate(string market, string granularity, DateTime fromDate);

        Task<List<CandleDto>> GetCandlesByMarketByFromDate(string market, DateTime fromDate);

        Task<List<PriceDto>> GetPricesByMarketByFromDate(string market, string granularity, DateTime fromDate);

        Task AddTrade(TradeDto trade);
    }
}