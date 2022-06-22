using Contact.Configuration;
using Contact.Models;
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
using Microsoft.EntityFrameworkCore;

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

            services.AddMassTransit(x =>
            {
                x.AddSagaStateMachine<OrderSaga, OrderSagaData>()
                    .InMemoryRepository()
                    .Endpoint(e =>
                    {
                        e.Name = rabbitConfiguration.ReceiveEndpoint;
                    });
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), hostConfigurator =>
                    {
                        hostConfigurator.Username(rabbitConfiguration.Username);
                        hostConfigurator.Password(rabbitConfiguration.Password);
                    });
                    cfg.ReceiveEndpoint(rabbitConfiguration.ReceiveEndpoint, ep =>
                    {
                        ep.ConfigureSaga<OrderSagaData>(context);
                    });
                });
                //x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                //{
                //    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), hostConfigurator =>
                //    {
                //        hostConfigurator.Username(rabbitConfiguration.Username);
                //        hostConfigurator.Password(rabbitConfiguration.Password);
                //    });
                //    //cfg.ReceiveEndpoint(rabbitConfiguration.ReceiveEndpoint, ep =>
                //    //{
                //    //    ep.StateMachineSaga(new OrderSaga(), new InMemorySagaRepository<OrderSagaData>());
                //    //});
                //}));
            });

            services.AddDbContext<OrderContext>(opt => opt.UseInMemoryDatabase("ContactOrderList"));
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
