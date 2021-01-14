using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpPriceLevelRepository
    {
        Task UpdatePriceLevel(PriceLevelDto level);

        Task<List<PriceLevelDto>> GetPriceLevelsByMarketByFromDate(string market, DateTime fromDate);
        Task<List<PriceLevelDto>> GetPriceLevelsByCurrentAndPreviousDay(string market, string granularity);

        //Task<List<PriceLevelDto>> GetPriceLevelCurrentAndPreviousDay(string market,string granularity);
        Task UpdatePriceLevels(List<PriceLevelDto> levels);
    }
}