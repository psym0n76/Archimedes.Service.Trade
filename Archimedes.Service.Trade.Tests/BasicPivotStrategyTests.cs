using System;
using System.Collections.Generic;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Http;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class BasicPivotStrategyTests
    {


        [Test]
        public void Should_()
        {
            
        }



        private IBasicPivotStrategy GetSubjectUnderTest()
        {

            var mockLogger = new Mock<ILogger<BasicPivotStrategy>>();
            var mockHttp = new Mock<IHttpRepositoryClient>();
            var mockMapper = new Mock<IMapper>();


            mockHttp.Setup(a =>
                a.GetPriceLevelsByMarketByGranularityByFromDate(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>())).ReturnsAsync(GetPriceLevels());

            var mockPriceSubscriber = new Mock<IPriceSubscriber>();
            var mockCandleSubscriber = new Mock<ICandleSubscriber>();
            var mockPriceLevelSubscriber = new Mock<IPriceLevelSubscriber>();
        

            return new BasicPivotStrategy(mockLogger.Object, mockHttp.Object, mockMapper.Object,
                mockPriceSubscriber.Object, mockCandleSubscriber.Object, mockPriceLevelSubscriber.Object);

        }

        private List<PriceLevelDto> GetPriceLevels()
        {
            var data = new FileReader();
            var priceLevels = data.Reader<PriceLevelDto>("GBPUSD_15Min_20201001_PIVOT7");

            //2020-10-07T22:45:00 LOW PIVOT BUY
            return priceLevels;
        }

        private List<Library.Candles.Price> GetPrices()
        {
            return new List<Library.Candles.Price>();
        }
    }
}