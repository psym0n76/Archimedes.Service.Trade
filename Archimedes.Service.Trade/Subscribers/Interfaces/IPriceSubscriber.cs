﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;

namespace Archimedes.Service.Price
{
    public interface IPriceSubscriber
    {
        event EventHandler<MessageHandlerEventArgs> PriceMessageEventHandler;
        Task Consume(CancellationToken cancellationToken);
    }
}