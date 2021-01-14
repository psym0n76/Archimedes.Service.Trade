using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;
using Microsoft.AspNetCore.SignalR;

namespace Archimedes.Service.Trade.Hubs
{
    public class PriceLevelHub : Hub<IPriceLevelHub>
    {
        public async Task Add(PriceLevelDto value)
        {
            await Clients.All.Add(value);
        }

        public async Task Delete(PriceLevelDto value)
        {
            await Clients.All.Delete(value);
        }

        public async Task Update(PriceLevelDto value)
        {
            await Clients.All.Update(value);
        }
    }
}