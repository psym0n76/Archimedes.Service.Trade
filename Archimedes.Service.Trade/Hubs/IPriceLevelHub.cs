using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Hubs
{
    public interface IPriceLevelHub
    {
        Task Add(PriceLevelDto value);
        Task Delete(PriceLevelDto value);
        Task Update(PriceLevelDto value);
    }
}