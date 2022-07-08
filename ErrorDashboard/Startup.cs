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
                        ep.Handler<Fault<NewBookSalesInfo>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<NewBookMarketingInfo>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoicePaymentTimeoutExpired>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ContactOrderConfirmationTimeoutExpired>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ContactOrderPaymentTimeoutExpired>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ContactShipmentTimeoutExpired>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ContactConfirmationConfirmedByAllParties>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ContactConfirmationRefusedByAtLeastOneParty>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<OrderStart>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<OrderCancel>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ClientConfirmationAccept>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ClientConfirmationRefuse>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseConfirmation>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseConfirmationAccept>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseConfirmationRefuse>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<SalesConfirmation>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<SalesConfirmationAccept>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<SalesConfirmationRefuse>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<MarketingConfirmation>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<MarketingConfirmationAccept>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<MarketingConfirmationRefuse>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingConfirmation>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingConfirmationAccept>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingConfirmationRefuse>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoiceStart>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoicePublish>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoiceCancel>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoicePaid>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<AccountingInvoiceNotPaid>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingShipmentStart>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingShipmentSent>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingShipmentNotSent>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<ShippingWarehouseDeliveryConfirmationTimeoutExpired>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseDeliveryStart>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseDeliveryStartConfirmation>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                        ep.Handler<Fault<WarehouseDeliveryStartRejection>>(context =>
                        {
                            Console.Out.WriteLine($"[ERROR] {context.Message.Timestamp}: \n" +
                                $"\t{context.Message.Message.Print()}");
                            PrintDetails(context);
                            return Task.CompletedTask;
                        });
                    });
                });
            });
        }

        public void Configure() { }
    }
}
