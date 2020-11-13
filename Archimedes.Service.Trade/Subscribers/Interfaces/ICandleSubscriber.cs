using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;

namespace Archimedes.Service.Trade
{
    public interface ICandleSubscriber
    {
        //event EventHandler<CandleMessageEventArgs> CandleMessageEventHandler;
        event EventHandler<MessageHandlerEventArgs> CandleMessageEventHandler;

        Task Consume(CancellationToken cancellationToken);
    }
}