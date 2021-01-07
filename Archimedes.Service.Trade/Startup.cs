using System.Threading;
using Archimedes.Library.Candles;
using Archimedes.Library.Domain;
using Archimedes.Library.Message;
using Archimedes.Library.Price;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Strategies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoMapper;

namespace Archimedes.Service.Trade
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddTransient<ICacheManager, CacheManager>();

            services.Configure<Config>(Configuration.GetSection("AppSettings"));
            services.AddSingleton(Configuration);
            
            var config = Configuration.GetSection("AppSettings").Get<Config>();

            // replace this with model_runner queue names
            services.AddTransient<IPriceFanoutConsumer>(x =>
                new PriceFanoutConsumer(config.RabbitHost, config.RabbitPort, "Archimedes_Price"));

            services.AddTransient<IPriceLevelFanoutConsumer>(x =>
                new PriceLevelFanoutConsumer(config.RabbitHost, config.RabbitPort, "Archimedes_Price_Level"));

            services.AddTransient<ICandleFanoutConsumer>(x =>
                new CandleFanoutConsumer(config.RabbitHost, config.RabbitPort, "Archimedes_Candle_GBPUSD.5Min"));

            services.AddTransient<IProducer<TradeMessage>>(x =>
                new Producer<TradeMessage>(config.RabbitHost, config.RabbitPort, config.RabbitExchange));

            services.AddTransient<IStrategyRunner, StrategyRunner>();


            services.AddTransient<IEngulfingCandleStrategy, BasicEngulfingCandleStrategy>();
            services.AddTransient<IBasicPriceLevelStrategy, BasicPriceLevelStrategy>();
            services.AddTransient<IBasicPriceStrategy, BasicPriceStrategy>();
            services.AddTransient<IBasicPriceStrategyHistoryUpdater, BasicPriceStrategyHistoryUpdater>();

            services.AddTransient<IPriceSubscriber, PriceSubscriber>();
            services.AddTransient<IPriceLevelSubscriber, PriceLevelSubscriber>();
            services.AddTransient<ICandleSubscriber, CandleSubscriber>();

            services.AddTransient<ICandleLoader, CandleLoader>();
            services.AddTransient<IPriceLevelLoader, PriceLevelLoader>();
            services.AddTransient<ICandleHistoryLoader, CandleHistoryLoader>();
            services.AddTransient<ICandleLoader, CandleLoader>();
            services.AddTransient<ILastPriceLoader, LastPriceLoader>();

            services.AddTransient<IPriceAggregator>(x => new PriceAggregator(10));

            services.AddTransient<ITradeValuation, TradeValuation>();
            services.AddTransient<IPriceTradeExecutor, PriceTradeExecutor>();
            services.AddTransient<ITradeGenerator, TradeGenerator>();

            services.AddTransient<ITradeProfileFactory, TradeProfileFactory>();

            services.AddAutoMapper(typeof(Startup));
            services.AddLogging();

            //services.Add<TradeProfileFactory>();
            //services.AddScoped<TradeProfileRiskThreeTimesEqual>()
            //    .AddScoped<ITradeProfile, TradeProfileRiskThreeTimesEqual>(s => s.GetService<TradeProfileRiskThreeTimesEqual>());


            services.AddHttpClient<IHttpTradeRepository, HttpTradeRepository>();
            services.AddHttpClient<IHttpPriceRepository, HttpPriceRepository>();
            services.AddHttpClient<IHttpPriceLevelRepository, HttpPriceLevelRepository>();
            services.AddHttpClient<IHttpCandleRepository, HttpCandleRepository>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}