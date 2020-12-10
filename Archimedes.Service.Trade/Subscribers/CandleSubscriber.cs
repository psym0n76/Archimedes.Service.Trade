using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Price
{
    public class CandleSubscriber : ICandleSubscriber
    {
        public event EventHandler<CandleMessageHandlerEventArgs> CandleMessageEventHandler;
        private readonly ICandleFanoutConsumer _consumer;
        private readonly ILogger<CandleSubscriber> _logger;

        public CandleSubscriber(ICandleFanoutConsumer consumer, ILogger<CandleSubscriber> logger)
        {
            _consumer = consumer;
            _logger = logger;
            _consumer.HandleMessage += Consumer_HandleMessage; ;
        }

        private void Consumer_HandleMessage(object sender, CandleMessageHandlerEventArgs e)
        {
            _logger.LogInformation($"Received Candle Update {e.Message}");
            CandleMessageEventHandler?.Invoke(sender, e);
        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscribed to Candle");
            _consumer.Subscribe(cancellationToken);
            return Task.CompletedTask;
        }
    }
}