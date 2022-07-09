using Common;
using ErrorDashboard.Configuration;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ErrorDashboard
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

            services.AddMassTransit(x =>
            {
                x.AddConsumer<FaultConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("error-dashboard", ep =>
                    {
                        ep.ConfigureConsumer<FaultConsumer>(context);
                    });
                });
            });
        }

        public void Configure() { }

        public class FaultConsumer : IConsumer<Fault<BaseMessage>>
        {
            private readonly ILogger<FaultConsumer> _logger;
            public FaultConsumer(ILogger<FaultConsumer> logger)
            {
                _logger = logger;
            }
            public Task Consume(ConsumeContext<Fault<BaseMessage>> context)
            {
                _logger.LogError($"[ERROR] {context.Message.Timestamp}: \n");
                PrintDetails(context);
                return Task.CompletedTask;
            }

            private void PrintDetails(ConsumeContext<Fault> context)
            {
                foreach (KeyValuePair<string, object> header in context.Headers.GetAll())
                {
                    _logger.LogError($"\t{header.Key}: {header.Value}");
                }
                _logger.LogError("\tExceptions:");
                foreach (ExceptionInfo ex in context.Message.Exceptions)
                {
                    _logger.LogError($"{ex.StackTrace}");
                }
            }
        }
    }
}
