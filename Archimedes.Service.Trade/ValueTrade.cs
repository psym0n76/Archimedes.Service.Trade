using System;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class ValueTrade : IValueTrade
    {
        public event EventHandler<PriceEventArgs> HandleMessage;
        private readonly ILogger<ValueTrade> _logger;

        public ValueTrade(ILogger<ValueTrade> logger)
        {
            _logger = logger;
            HandleMessage += ValueTrade_HandleMessage;
        }

        private void ValueTrade_HandleMessage(object sender, PriceEventArgs e)
        {
            _logger.LogInformation("R message");
        }
    }
}