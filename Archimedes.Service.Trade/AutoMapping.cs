using Archimedes.Library.Message.Dto;
using AutoMapper;

namespace Archimedes.Service.Trade
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<PriceLevel, PriceLevelDto>();
            CreateMap<PriceLevelDto, PriceLevel>();
        }
    }
}