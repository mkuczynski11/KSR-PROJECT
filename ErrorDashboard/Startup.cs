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
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("error-dashboard", ep =>
                    {
                        ep.ConfigureConsumer<FaultConsumer<NewBookSalesInfo>>(context);
                        ep.ConfigureConsumer<FaultConsumer<NewBookMarketingInfo>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoicePaymentTimeoutExpired>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ContactOrderClientConfirmationTimeoutExpired>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ContactOrderPaymentTimeoutExpired>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ContactShipmentTimeoutExpired>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ContactConfirmationConfirmedByAllParties>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ContactConfirmationRefusedByAtLeastOneParty>>(context);
                        ep.ConfigureConsumer<FaultConsumer<OrderStart>>(context);
                        ep.ConfigureConsumer<FaultConsumer<OrderCancel>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ClientConfirmationAccept>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ClientConfirmationRefuse>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseConfirmation>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseConfirmationAccept>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseConfirmationRefuse>>(context);
                        ep.ConfigureConsumer<FaultConsumer<SalesConfirmation>>(context);
                        ep.ConfigureConsumer<FaultConsumer<SalesConfirmationAccept>>(context);
                        ep.ConfigureConsumer<FaultConsumer<SalesConfirmationRefuse>>(context);
                        ep.ConfigureConsumer<FaultConsumer<MarketingConfirmation>>(context);
                        ep.ConfigureConsumer<FaultConsumer<MarketingConfirmationAccept>>(context);
                        ep.ConfigureConsumer<FaultConsumer<MarketingConfirmationRefuse>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingConfirmation>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingConfirmationAccept>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingConfirmationRefuse>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoiceStart>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoicePublish>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoiceCancel>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoicePaid>>(context);
                        ep.ConfigureConsumer<FaultConsumer<AccountingInvoiceNotPaid>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingShipmentStart>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingShipmentSent>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingShipmentNotSent>>(context);
                        ep.ConfigureConsumer<FaultConsumer<ShippingWarehouseDeliveryConfirmationTimeoutExpired>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseDeliveryStart>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseDeliveryStartConfirmation>>(context);
                        ep.ConfigureConsumer<FaultConsumer<WarehouseDeliveryStartRejection>>(context);
                    });
                });
            });
        }

        public void Configure() { }

        class FaultConsumer<T> : IConsumer<Fault<T>>
        {
            public Task Consume(ConsumeContext<Fault<T>> context)
            {
                Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n");
                PrintDetails(context);
                return Task.CompletedTask;
            }

            private void PrintDetails(ConsumeContext<Fault> context)
            {
                foreach (KeyValuePair<string, object> header in context.Headers.GetAll())
                {
                    Console.Out.WriteLine($"\t{header.Key}: {header.Value}");
                }
                Console.Out.WriteLine("\tExceptions:");
                foreach (ExceptionInfo ex in context.Message.Exceptions)
                {
                    Console.Out.WriteLine($"{ex.StackTrace}");
                }
            }
        }
    }
}
