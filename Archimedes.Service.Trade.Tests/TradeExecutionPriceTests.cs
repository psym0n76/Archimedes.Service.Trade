using System.Collections.Generic;
using System.Threading;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Strategies;
using Archimedes.Service.Trade.Trade;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class TradeExecutionPriceTests
    {

        [Test]
        [Ignore("Ignored")]
        public void Should_UpdatePriceLevel_When_Price_Crosses_PriceLevel()
        {
            var mockHttpClient = new Mock<IHttpPriceLevelRepository>();
            var mockLogger = new Mock<ILogger<PriceTradeExecutor>>();
            var mockCache = new Mock<ICacheManager>();

            var mockTradeProfile = new Mock<ITradeProfileFactory>();

            var p = new TradeParameters()
            {
                BuySell = ""
            };

            var trans = new Transaction(p)
            {
                Closed = false
            };

            //mockTradeProfile.Setup(a=>a.GetTradeGenerationService(It.IsAny<string>()).Generate(It.IsAny<PriceDto>(),It.IsAny<string>())).Returns(trans);

            var priceLevels = new List<PriceLevelDto>()
            {
                new PriceLevelDto()
                {
                    BuySell = "BUY",
                    AskPrice = 1.3000m
                },
                new PriceLevelDto()
                {
                    BuySell = "SELL",
                    BidPrice = 1.2000m
                }
            };

            var lastPrice = new PriceDto() {Ask = 1.3001m, Bid = 1.1999m};
            
            mockCache.Setup(x => x.GetAsync<List<PriceLevelDto>>("price-levels")).ReturnsAsync(priceLevels);

            mockCache.Setup(x => x.GetAsync<PriceDto>("price"))
                .ReturnsAsync(lastPrice);



            var subject = new PriceTradeExecutor(mockLogger.Object, mockHttpClient.Object,mockCache.Object);

            var price = new PriceDto() {Ask = 1.2999m, Bid = 1.2001m};

            subject.Execute(price, 0.0000m);

            mockHttpClient.Verify(a=>a.UpdatePriceLevel(It.IsAny<PriceLevelDto>()),Times.Exactly(2));
        }
    }
}