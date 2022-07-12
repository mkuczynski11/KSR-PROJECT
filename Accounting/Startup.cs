using Accounting.Configuration;
using Accounting.Models;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using HealthChecks.UI.Client;

namespace Accounting
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
                x.AddSagaStateMachine<InvoiceSaga, InvoiceSagaData>()
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
                    cfg.ReceiveEndpoint(endpointConfiguration.InvoiceSaga, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureSaga<InvoiceSagaData>(context);
                    });
                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                });
            });

            services.AddHealthChecks()
                .AddMongoDb(mongodbConnectionString: mongoDbConfiguration.Connection, name: "mongoDB", failureStatus: HealthStatus.Unhealthy)
                .AddRabbitMQ(rabbitConnectionString: rabbitConfiguration.ConnStr);
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

                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}
