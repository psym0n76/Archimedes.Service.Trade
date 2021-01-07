using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.Price;
using Archimedes.Library.RabbitMq;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Price
{
    public class PriceSubscriber : IPriceSubscriber
    {
        public event EventHandler<PriceMessageHandlerEventArgs> PriceMessageEventHandler; 
        private readonly IPriceFanoutConsumer _consumer;
        private readonly ILogger<PriceSubscriber> _logger;
        private readonly IPriceAggregator _aggregator;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public PriceSubscriber(IPriceFanoutConsumer consumer, ILogger<PriceSubscriber> logger, IPriceAggregator aggregator)
        {
            _consumer = consumer;
            _logger = logger;
            _aggregator = aggregator;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, PriceMessageHandlerEventArgs e)
        {
            try
            {
                _logId = _batchLog.Start();

                _aggregator.Add(e.Prices);

                if (_aggregator.SendPrice())
                {
                    AggregatePrice(sender, e);
                    _logger.LogInformation(_batchLog.Print(_logId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(_batchLog.Print(_logId,
                    $"Error returned from PriceSubscriber Price Bid {e.Prices[0].Bid} Ask: {e.Prices[0].Bid} {e.Prices[0].TimeStamp}", ex));
            }
        }

        private void AggregatePrice(object sender, PriceMessageHandlerEventArgs e)
        {
            var prices = _aggregator.GetLowBidAndAskHigh();

            _batchLog.Update(_logId,
                $"Aggregating BidAsk from {_aggregator.AggregatorCount()} Prices Bid: {prices.Bid} Ask: {prices.Ask} {prices.TimeStamp}");

            PriceMessageEventHandler?.Invoke(sender, new PriceMessageHandlerEventArgs() {Message = e.Message, Prices = new List<PriceDto> { prices}});

        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Subscribed to Prices");
            _consumer.Subscribe(cancellationToken);
            return Task.CompletedTask;
        }
    }
}