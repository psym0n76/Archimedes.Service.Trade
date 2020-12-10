using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Price
{
    public class PriceSubscriber : IPriceSubscriber
    {
        public event EventHandler<PriceMessageHandlerEventArgs> PriceMessageEventHandler; 
        private readonly IPriceFanoutConsumer _consumer;
        private readonly ILogger<PriceSubscriber> _logger;

        public PriceSubscriber(IPriceFanoutConsumer consumer, ILogger<PriceSubscriber> logger)
        {
            _consumer = consumer;
            _logger = logger;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, PriceMessageHandlerEventArgs e)
        {
            _logger.LogInformation($"Received Price Update {e.Message}");
            PriceMessageEventHandler?.Invoke(sender, e);
        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscribed to Prices");
            _consumer.Subscribe(cancellationToken);
            return Task.CompletedTask;
        }
    }
}