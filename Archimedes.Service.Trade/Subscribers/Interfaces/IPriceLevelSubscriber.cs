using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;

namespace Archimedes.Service.Trade
{
    public interface IPriceLevelSubscriber
    {
        //event EventHandler<CandleMessageEventArgs> CandleMessageEventHandler;
        event EventHandler<MessageHandlerEventArgs> PriceLevelMessageEventHandler;

        Task Consume(CancellationToken cancellationToken);
    }
}