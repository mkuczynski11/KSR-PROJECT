using Contact.Configuration;
using Contact.Models;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Contact
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var endpointConfiguration = Configuration.GetSection("Endpoint").Get<EndpointConfiguration>();
            var mongoDbConfiguration = Configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

            services.AddMassTransit(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaData>()
                    .MongoDbRepository(r =>
                    {
                        r.Connection = mongoDbConfiguration.Connection;
                        r.DatabaseName = mongoDbConfiguration.DatabaseName;
                        r.CollectionName = mongoDbConfiguration.CollectionName.Saga;
                    });
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), hostConfigurator =>
                    {
                        hostConfigurator.Username(rabbitConfiguration.Username);
                        hostConfigurator.Password(rabbitConfiguration.Password);
                    });
                    cfg.ReceiveEndpoint(endpointConfiguration.OrderSaga, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureSaga<OrderSagaData>(context);
                    });
                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                    
                });
            });

            services.AddSingleton(new MongoClient(mongoDbConfiguration.Connection));
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
