using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;

namespace Archimedes.Service.Price
{
    public class PriceLevelSubscriber : IPriceLevelSubscriber
    {
        public event EventHandler<MessageHandlerEventArgs> PriceLevelMessageEventHandler; 
        private readonly IPriceConsumer _consumer;

        public PriceLevelSubscriber(IPriceConsumer consumer)
        {
            _consumer = consumer;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            PriceLevelMessageEventHandler?.Invoke(this, args);
        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe();
            return Task.CompletedTask;
        }
    }
}