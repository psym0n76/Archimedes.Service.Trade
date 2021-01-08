using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Http
{
    public interface IHttpTradeRepository
    {
        Task AddTrades(List<TradeDto> trade);
        Task UpdateTrade(TradeDto trade);
        Task UpdateTrades(List<TradeDto> trades);
    }
}