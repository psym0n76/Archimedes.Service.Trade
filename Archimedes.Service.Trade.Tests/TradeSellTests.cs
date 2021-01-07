using Archimedes.Library;
using NUnit.Framework;

namespace Archimedes.Service.Trade.Tests
{
    [TestFixture]
    public class TradeSellTests
    {
        [Test]
        public void Should_Load_Price_Into_Trade()
        {
            var trade = GetSellTrade();
            Assert.IsNotNull(trade);
        }

        [Test]
        public void Should_Add_Three_Profit_Targets()
        {
            var trade = GetSellTrade();
            Assert.AreEqual(3,trade.ProfitTargets.Count);
        }

        [Test]
        public void Should_Add_Three_ProfitTargets_WithPrice()
        {
            var trade = GetSellTrade();

            Assert.AreEqual(1.2995m,trade.ProfitTargets[0].TargetPrice);
            Assert.AreEqual(1.2990m,trade.ProfitTargets[1].TargetPrice);
            Assert.AreEqual(1.2985m,trade.ProfitTargets[2].TargetPrice);
        }

        [Test]
        public void Should_Add_One_StopTarget()
        {
            var trade = GetSellTrade();
            Assert.AreEqual(1,trade.StopTargets.Count);
        }

        [Test]
        public void Should_Add_One_StopTarget_WithPrice()
        {
            var trade = GetSellTrade();
            Assert.AreEqual(1.3005m,trade.StopTargets[0].TargetPrice);
        }

        [Test]
        public void Should_Set_MaxPriceToCurrentPrice()
        {
            var trade = GetSellTrade();

            trade.SetMaxPrice(1.2900m);
            trade.SetMaxPrice(1.2800m);
            trade.SetMaxPrice(1.3100m);

            Assert.AreEqual(1.2800m,trade.MaxPrice);
        }


        private static TradeTransaction GetSellTrade()
        {
            var tradeParams = new TradeParameters()
            {
                BuySell = "SELL",
                RiskReward = 3,
                EntryPrice = 1.30m,
                SpreadAsPips = 5,
                Market = "GBP/USD",
                TradeCounter = 3
            };

            return new TradeTransaction(tradeParams);
        }
    }
}