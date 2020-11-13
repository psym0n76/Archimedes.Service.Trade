using Archimedes.Library.Domain;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade.Http;
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
            services.Configure<Config>(Configuration.GetSection("AppSettings"));
            services.AddSingleton(Configuration);
            var config = Configuration.GetSection("AppSettings").Get<Config>();

            services.AddTransient<IPriceFanoutConsumer>(x =>
                new PriceFanoutConsumer(config.RabbitHost, config.RabbitPort, "Archimedes_Price"));

            services.AddTransient<IPriceConsumer>(x =>
                new PriceConsumer(config.RabbitHost, config.RabbitPort, config.RabbitExchange,""));

            services.AddTransient<ICandleConsumer>(x =>
                new CandleConsumer(config.RabbitHost, config.RabbitPort, config.RabbitExchange,"Candle_Producer"));

            services.AddAutoMapper(typeof(Startup));
            services.AddLogging();

            services.AddHttpClient<IHttpRepositoryClient, HttpRepositoryClient>();

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
