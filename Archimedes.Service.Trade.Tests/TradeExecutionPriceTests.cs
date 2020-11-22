using System.Collections.Generic;
using System.Threading;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Phema.Caching;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class TradeExecutionPriceTests
    {
        [Test]
        public void Should_UpdatePriceLevel_When_Price_Crosses_PriceLevel()
        {
            var mockHttpClient = new Mock<IHttpRepositoryClient>();
            var mockLogger = new Mock<ILogger<TradeExecutorPrice>>();
            var mockDistributedCache = new Mock<IDistributedCache<List<PriceLevel>>>();

            var priceLevels = new List<PriceLevel>()
            {
                new PriceLevel()
                {
                    BuySell = "BUY",
                    AskPrice = 1.3000m
                },
                new PriceLevel()
                {
                    BuySell = "SELL",
                    BidPrice = 1.2000m

                }
            };

            mockDistributedCache.Setup(x => x.GetAsync(It.IsAny<string>(), new CancellationToken()))
                .ReturnsAsync(priceLevels);


            var subject = new TradeExecutorPrice(mockLogger.Object, mockDistributedCache.Object, mockHttpClient.Object);

            var lastBidPrice = 1.1999m;
            var lastAskPrice = 1.3001m;
            var price = new PriceDto() {Ask = 1.2999m, Bid = 1.2001m};

            subject.Execute(price, lastBidPrice, lastAskPrice);

            mockHttpClient.Verify(a=>a.UpdatePriceLevel(It.IsAny<PriceLevelDto>()),Times.Exactly(2));
        }
    }
}