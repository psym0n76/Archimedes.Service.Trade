using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public interface ICandleLoader
    {
        Task<List<CandleDto>> LoadAsync(string market, string granularity, DateTime startDate);
        Task<List<CandleDto>> LoadCandlesPreviousFiveAsync(string market, string granularity, DateTime messageStartDate);

        Task ValidateRecentCandle(string market, string granularity, int retries);
    }
}