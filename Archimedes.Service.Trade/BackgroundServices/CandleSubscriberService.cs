using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class CandleSubscriberService : BackgroundService
    {
        private readonly ICandleSubscriber _candleSubscriber;
        private readonly ILogger<CandleSubscriberService> _logger;

        public CandleSubscriberService(ICandleSubscriber candleSubscriber, ILogger<CandleSubscriberService> logger)
        {
            _candleSubscriber = candleSubscriber;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _candleSubscriber.Consume(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unknown error found in CandleSubscriberService {e.Message} {e.StackTrace}");
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }
    }
}