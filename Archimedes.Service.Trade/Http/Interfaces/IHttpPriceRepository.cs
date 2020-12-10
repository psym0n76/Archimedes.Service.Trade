using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpPriceRepository
    {
        Task<List<PriceDto>> GetPricesByMarketByFromDate(string market, string granularity, DateTime fromDate);
        Task<PriceDto> GetLastPriceByMarket(string market);
    }
}