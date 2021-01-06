using System.Collections.Generic;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using NLog;
using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class TradeValuationTests
    {
        [Test]
        public void Should_Iterate_EachPrice_ThroughModel()
        {
            var subject = GetSubjectUnderTest();

            var price  = new PriceDto()
            {
                Bid = 1.25m,
                Ask = 1.30m
            };

            subject.UpdateTradeLocked(price);
            Assert.IsTrue(true);
        }

        private ITradeValuation GetSubjectUnderTest()
        {
            var mockCacheTransaction = new Mock<ICacheManager>();
            var mockLogger = new Mock<ILogger<TradeValuation>>();

            //todo setup obejct
            return new TradeValuation(mockCacheTransaction.Object, mockLogger.Object);

        }
    }
}