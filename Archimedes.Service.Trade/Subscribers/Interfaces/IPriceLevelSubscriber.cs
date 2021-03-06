﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;

namespace Archimedes.Service.Trade
{
    public interface IPriceLevelSubscriber
    {
        event EventHandler<PriceLevelMessageHandlerEventArgs> PriceLevelMessageEventHandler;

        Task Consume(CancellationToken cancellationToken);
    }
}