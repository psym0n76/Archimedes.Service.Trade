using System;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeProfileFactory: ITradeProfileFactory
    {

        private readonly IServiceProvider _serviceProvider;

        public TradeProfileFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITradeProfile GetTradeGenerationService(string tradeProfile)
        {

            switch (tradeProfile)
            {
                case "TradeProfileRiskThreeTimesEqual":

                    //return (ITradeProfile) _serviceProvider.GetService(typeof(TradeProfileRiskThreeTimesEqual));
                    return new TradeProfileRiskThreeTimesEqual();


                default:
                    return default;
            }
        }
    }
}