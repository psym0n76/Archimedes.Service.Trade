﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.RabbitMq;

namespace Archimedes.Service.Price
{
    public class PriceSubscriber : IPriceSubscriber
    {
        public event EventHandler<MessageHandlerEventArgs> PriceMessageEventHandler; 
        private readonly IPriceConsumer _consumer;

        public PriceSubscriber(IPriceConsumer consumer)
        {
            _consumer = consumer;
            _consumer.HandleMessage += Consumer_HandleMessage;
        }

        private void Consumer_HandleMessage(object sender, MessageHandlerEventArgs args)
        {
            PriceMessageEventHandler?.Invoke(this, args);
        }

        public Task Consume(CancellationToken cancellationToken)
        {
            _consumer.Subscribe();
            return Task.CompletedTask;
        }
    }
}