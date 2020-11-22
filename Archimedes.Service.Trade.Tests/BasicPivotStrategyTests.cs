using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Phema.Caching;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class BasicPivotStrategyTests
    {
        [Test]
        public void Should_Load_Price_AndPriceLevels_FromFile()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var priceLevels = GetPriceLevels();
            TestContext.Out.WriteLine($"Loaded {priceLevels.Count} in Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");

            var prices = GetPrices();
            TestContext.Out.WriteLine($"Loaded {prices.Count} in Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");

            Assert.True(priceLevels.Any());
            Assert.True(prices.Any());
        }


        [Test]
        public void Should_Iterate_EachPrice_ThroughModel()
        {
            var subject = GetSubjectUnderTest();
            var prices = GetPrices();

            var candles = new List<CandleDto>();

            var priceLevels = GetPriceLevels();
            subject.Consume(priceLevels, candles);

            foreach (var price in prices)
            {
                subject.UpdateTrade(price);
            }

            Assert.IsTrue(true);
        }

        private IBasicPivotStrategy GetSubjectUnderTest()
        {
            var mockLogger = new Mock<ILogger<BasicPivotStrategy>>();


            var mockPriceSubscriber = new Mock<IPriceSubscriber>();
            var mockCandleSubscriber = new Mock<ICandleSubscriber>();
            var mockPriceLevelSubscriber = new Mock<IPriceLevelSubscriber>();
            var mockTradeExecutor = new Mock<ITradeExecutorPrice>();
            var mockCache = new Mock<IDistributedCache<List<PriceLevel>>>();
            
            return new BasicPivotStrategy(mockLogger.Object,mockPriceSubscriber.Object, mockCandleSubscriber.Object, mockPriceLevelSubscriber.Object,mockTradeExecutor.Object, mockCache.Object);

        }

        private List<PriceLevel> GetPriceLevels()
        {
            var data = new FileReader();
            var priceLevels = data.Reader<PriceLevel>("GBPUSD_15Min_20201101_PIVOT7");

            //2020-10-07T22:45:00 LOW PIVOT BUY
            return priceLevels;
        }

        private List<PriceDto> GetPrices()
        {
            var data = new FileReader();
            var prices = data.Reader<PriceDto>("GBPUSD_0Min_20201116_0900-1930");

            return prices;
        }
    }
}