using System;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public class PriceLevel : PriceLevelDto
    {
        public int Trades { get; set; }
        public bool LevelBroken { get; set; }
        public DateTime LevelBrokenDate { get; set; }
    }
}