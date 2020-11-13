using Archimedes.Library.Message.Dto;
using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    public class PriceTargetTests
    {
        [Test]
        public void Should_Set_Trade_To_Closed_When_Target_Profit_Hit()
        {
            var priceTarget = new TransactionPriceTarget()
            {
                EntryPrice = 1.31m,
                TargetPrice = 1.32m,
                Closed = false
            };

            priceTarget.UpdateTrade(new PriceDto(){Bid = 1.33m});

            Assert.AreEqual(true,priceTarget.Closed);
        }

        [Test]
        public void Should_Set_Trade_To_Closed_When_Close_Price_Hit()
        {
            var priceTarget = new TransactionPriceTarget()
            {
                EntryPrice = 1.31m,
                TargetPrice = 1.30m,
                Closed = false
            };

            priceTarget.UpdateTrade(new PriceDto(){Ask = 1.29m});
            Assert.AreEqual(true,priceTarget.Closed);
        }


    }
}