using System;
using System.Collections.Generic;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Http;
using AutoMapper;

namespace Archimedes.Service.Trade
{
    public class StrategyRunner
    {
        private readonly IBasicPivotStrategy _basicPivotStrategy;
        private readonly IHttpRepositoryClient _client;
        private readonly IMapper _mapper;

        public StrategyRunner(IBasicPivotStrategy basicPivotStrategy, IHttpRepositoryClient client, IMapper mapper)
        {
            _basicPivotStrategy = basicPivotStrategy;
            _client = client;
            _mapper = mapper;
        }

        public async void Run(string market, string granularity)
        {
            var priceLevelDto =
                await _client.GetPriceLevelsByMarketByGranularityByFromDate(market, granularity,
                    DateTime.Now.PreviousWorkDay().AddDays(-1));

            var priceLevels = MapLevels(priceLevelDto);

            var candles = await _client.GetCandlesByMarketByFromDate(market, DateTime.Today.AddDays(-10));

            _basicPivotStrategy.Consume(priceLevels, candles);
        }

        private List<PriceLevel> MapLevels(List<PriceLevelDto> levels)
        {
            return _mapper.Map<List<PriceLevel>>(levels);
        }
    }
}