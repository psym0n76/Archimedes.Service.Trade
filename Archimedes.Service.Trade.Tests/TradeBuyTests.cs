using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class TradeBuyTests
    {
        [Test]
        public void Should_Load_Price_Into_Trade()
        {
            var trade = GetBuyTrade();
            Assert.IsNotNull(trade);
        }

        [Test]
        public void Should_Add_Three_Profit_Targets()
        {
            var trade = GetBuyTrade();
            Assert.AreEqual(3,trade.ProfitTargets.Count);
        }

        [Test]
        public void Should_Add_Three_ProfitTargets_WithPrice()
        {
            var trade = GetBuyTrade();

            Assert.AreEqual(1.3005m,trade.ProfitTargets[0].TargetPrice);
            Assert.AreEqual(1.3010m,trade.ProfitTargets[1].TargetPrice);
            Assert.AreEqual(1.3015m,trade.ProfitTargets[2].TargetPrice);
        }

        [Test]
        public void Should_Add_One_StopTarget()
        {
            var trade = GetBuyTrade();
            Assert.AreEqual(1,trade.StopTargets.Count);
        }

        [Test]
        public void Should_Add_One_StopTarget_WithPrice()
        {
            var trade = GetBuyTrade();
            Assert.AreEqual(1.2995m,trade.StopTargets[0].TargetPrice);
        }

        [Test]
        public void Should_Set_MaxPriceToCurrentPrice()
        {
            var trade = GetBuyTrade();

            trade.SetMaxPrice(1.3100m);
            trade.SetMaxPrice(1.3200m);
            trade.SetMaxPrice(1.3100m);

            Assert.AreEqual(1.3200m,trade.MaxPrice);
        }


        private static Transaction GetBuyTrade()
        {
            return new Transaction(5, 3, 1.30m, 3, "basic-split-profit-equally", 100, "buy", null);
        }
    }
}