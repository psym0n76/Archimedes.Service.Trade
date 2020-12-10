using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;

namespace Archimedes.Service.Trade
{
    public interface ICandleSubscriber
    {
        event EventHandler<CandleMessageHandlerEventArgs> CandleMessageEventHandler;

        Task Consume(CancellationToken cancellationToken);
    }
}