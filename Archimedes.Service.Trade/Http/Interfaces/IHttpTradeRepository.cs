using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpTradeRepository
    {
        Task AddTrade(List<TradeDto> trade);
    }
}