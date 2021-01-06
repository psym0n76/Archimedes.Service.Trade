using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public interface IPriceLevelLoader
    {
        Task<List<PriceLevelDto>> LoadAsync(string market, string granularity);
    }
}