using System.Collections.Generic;
using System.Threading;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IBasicCandleStrategy
    {
        void Consume(string tradeProfile, CancellationToken token);
    }
}