using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class PriceLevelSubscriberService : BackgroundService
    {
        private readonly IPriceLevelSubscriber _priceLevelSubscriber;
        private readonly ILogger<PriceLevelSubscriberService> _logger;

        public PriceLevelSubscriberService(IPriceLevelSubscriber priceLevelSubscriber, ILogger<PriceLevelSubscriberService> logger)
        {
            _priceLevelSubscriber = priceLevelSubscriber;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Subscribed to PriceLevel");
                    _priceLevelSubscriber.Consume(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unknown error found in PriceLevelSubscriberService {e.Message} {e.StackTrace}");
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }
    }
}