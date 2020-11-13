using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;

namespace Archimedes.Service.Price
{
    public class CandleSubscriber : ICandleSubscriber
    {
        public event EventHandler<MessageHandlerEventArgs> CandleMessageEventHandler; 
        private readonly ICandleConsumer _consumer;

        public CandleSubscriber(ICandleConsumer consumer)
        {
            _consumer = consumer;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            CandleMessageEventHandler?.Invoke(this, args);
        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(cancellationToken);
            return Task.CompletedTask;
        }
    }
}