using System.Collections.Generic;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;

namespace Archimedes.Service.Price
{
    public interface IBasicPivotStrategy
    {
        void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender, MessageHandlerEventArgs e);
        void CandleSubscriber_CandleMessageEventHandler(object sender, MessageHandlerEventArgs e);
        void PriceSubscriber_PriceMessageEventHandler(object sender, MessageHandlerEventArgs e);
        //void UpdateTransactionPriceTargets(PriceDto price);
        void Consume(List<PriceLevel> priceLevels, List<CandleDto> candles);
        void UpdatePriceLevel(List<PriceLevel> priceLevel);
        void UpdateCandles(List<CandleDto> candleDto);
        void UpdateTrade(PriceDto price);
    }
}