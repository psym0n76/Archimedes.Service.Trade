﻿using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IPriceTradeExecutor
    {
        void ExecuteLocked(PriceDto price, decimal tolerance);
    }
}