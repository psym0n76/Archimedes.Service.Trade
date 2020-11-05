using System;
using System.Threading;
using Archimedes.Library.Message;
using Archimedes.Library.RabbitMq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Archimedes.Service.Price
{
    public class PriceSubscriber : IPriceSubscriber
    {
        private readonly ILogger<PriceSubscriber> _logger;
        private readonly IPriceConsumer _consumer;

        public PriceSubscriber(ILogger<PriceSubscriber> log, IPriceConsumer consumer)
        {
            _logger = log;
            _consumer = consumer;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        public void Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe();
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            UpdateTrades(args);
        }

        private void UpdateTrades(MessageHandlerEventArgs args)
        {
            _logger.LogInformation($"Received from PriceExchange Message: {args.Message}");

            try
            {
                var message = JsonConvert.DeserializeObject<PriceMessage>(args.Message);
            }

            catch (JsonException j)
            {
                _logger.LogError($"Unable to Parse Price message {args.Message}{j.Message} {j.StackTrace}");
            }

            catch (Exception e)
            {
                _logger.LogError($"Unable to Post Price message to API {e.Message} {e.StackTrace}");
            }
        }
    }
}