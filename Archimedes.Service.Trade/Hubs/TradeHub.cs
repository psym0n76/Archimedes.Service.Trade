using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Microsoft.AspNetCore.SignalR;

namespace Archimedes.Service.Trade.Hubs
{
    public class TradeHub : Hub<ITradeHub>
    {
        public async Task Add(TradeDto value)
        {
            await Clients.All.Add(value);
        }

        public async Task Delete(TradeDto value)
        {
            await Clients.All.Delete(value);
        }

        public async Task Update(TradeDto value)
        {
            await Clients.All.Update(value);
        }
    }
}