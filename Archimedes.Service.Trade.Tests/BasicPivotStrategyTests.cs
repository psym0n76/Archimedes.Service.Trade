using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archimedes.Library.Message.Dto;
using NUnit.Framework;

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

        //private IBasicPivotStrategy GetSubjectUnderTest()
        //{
        //    var mockLogger = new Mock<ILogger<BasicPivotStrategy>>();

        //    var mockPriceSubscriber = new Mock<IPriceSubscriber>();
        //    var mockCandleSubscriber = new Mock<ICandleSubscriber>();
        //    var mockPriceLevelSubscriber = new Mock<IPriceLevelSubscriber>();

        //    var mockTradeExecutor = new Mock<ITradeExecutor>();
        //    var mockTradeValuation = new Mock<ITradeValuation>();
        //    var mockCandleLoader = new Mock<ICandleLoader>();

        //    var mockCache = new Mock<ICacheManager>();
        //    var mockFactory = new Mock<ITradeProfileFactory>();


        //    return new BasicPivotStrategy(mockPriceSubscriber.Object, mockCandleSubscriber.Object,
        //        mockPriceLevelSubscriber.Object, mockTradeExecutor.Object,mockTradeValuation.Object, mockCandleLoader.Object, mockFactory.Object, mockLogger.Object, mockCache.Object);

        //}

        private List<PriceLevelDto> GetPriceLevels()
        {
            var data = new FileReader();
            var priceLevels = data.Reader<PriceLevelDto>("GBPUSD_15Min_20201101_PIVOT7");

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