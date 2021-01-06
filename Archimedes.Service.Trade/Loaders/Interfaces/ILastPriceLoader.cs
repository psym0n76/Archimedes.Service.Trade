using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public interface ILastPriceLoader
    {
        Task<PriceDto> LoadAsync(string market);
    }
}