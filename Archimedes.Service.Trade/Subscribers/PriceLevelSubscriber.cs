using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Price
{
    public class PriceLevelSubscriber : IPriceLevelSubscriber
    {
        public event EventHandler<PriceLevelMessageHandlerEventArgs> PriceLevelMessageEventHandler; 
        private readonly IPriceLevelFanoutConsumer _consumer;
        private readonly ILogger<PriceLevelSubscriber> _logger;

        public PriceLevelSubscriber(IPriceLevelFanoutConsumer consumer, ILogger<PriceLevelSubscriber> logger)
        {
            _consumer = consumer;
            _logger = logger;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, PriceLevelMessageHandlerEventArgs e)
        {
            PriceLevelMessageEventHandler?.Invoke(sender, e);
            //_logger.LogInformation($"Success {counter}");
        }


        public Task Consume(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscribed to Price Level");
            _consumer.Subscribe(cancellationToken);
            return Task.CompletedTask;
        }
    }
}