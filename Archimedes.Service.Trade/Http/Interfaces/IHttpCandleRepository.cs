using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpCandleRepository
    {
        Task<List<CandleDto>> GetCandlesByMarketByFromDate(string market, DateTime fromDate);
    }
}