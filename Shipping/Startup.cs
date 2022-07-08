using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipping.Configuration;
using System;
using System.Linq;

using MassTransit;
using Shipping.Models;
using Microsoft.EntityFrameworkCore;
using Common;
using System.Threading.Tasks;

namespace Shipping
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
                x.AddSagaStateMachine<DeliveryStateMachine, DeliveryState>()
                    .InMemoryRepository();
                x.AddConsumer<ShippingConfirmationConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint(endpointConfiguration.ShippingSaga, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureSaga<DeliveryState>(context);
                    });

                    cfg.ReceiveEndpoint(endpointConfiguration.ConfirmationConsumer, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<ShippingConfirmationConsumer>(context);
                    });

                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                });
            });

            services.AddDbContext<ShippingContext>(opt => opt.UseInMemoryDatabase("ShippingInfo"));
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ShippingContext shippingContext)
        {
            AddShippingData(shippingContext);

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

        private static void AddShippingData(ShippingContext context)
        {
            var dpdMethod = new Method("DPD", 10.0);
            context.MethodItems.Add(dpdMethod);

            var ppMethod = new Method("Poczta polska", 13.0);
            context.MethodItems.Add(ppMethod);

            var ooMethod = new Method("Odbiór osobisty", 0.0);
            context.MethodItems.Add(ooMethod);

            context.SaveChanges();
        }

        public class DeliveryStateMachine : MassTransitStateMachine<DeliveryState>, IDisposable
        {
            private readonly IServiceScope _scope;

            public State RequestSend { get; private set; }
            public State ReadyToDeliver { get; private set; }
            public State WillNotDeliver { get; private set; }

            public Event<ShippingShipmentStart> ShippingShipmentStartEvent { get; private set; }
            public Event<WarehouseDeliveryStartConfirmation> WarehouseDeliveryConfirmationEvent { get; private set; }
            public Event<WarehouseDeliveryStartRejection> WarehouseDeliveryRejectionEvent { get; private set; }

            public Schedule<DeliveryState, ShippingWarehouseDeliveryConfirmationTimeoutExpired> ShippingWarehouseDeliveryConfirmationTimeout { get; private set; }

            public DeliveryStateMachine(IServiceProvider services, IConfiguration configuration)
            {
                _scope = services.CreateScope();

                var sagaConfiguration = configuration.GetSection("ShippingSaga").Get<ShippingSagaConfiguration>();

                InstanceState(x => x.CurrentState);

                Schedule(() => ShippingWarehouseDeliveryConfirmationTimeout, instance => instance.ShippingWarehouseDeliveryConfirmationTimeoutId, s =>
                {
                    s.Delay = TimeSpan.FromSeconds(sagaConfiguration.WarehouseDeliveryConfirmationTimeoutSeconds);
                    s.Received = r => r.CorrelateById(context => context.Message.ShippingId);
                });

                Initially(
                    When(ShippingShipmentStartEvent)
                    .Then(context => {
                        Console.WriteLine($"Got shipping request for book={context.Message.BookID}, quantity={context.Message.BookQuantity}, price={context.Message.DeliveryPrice}, method={context.Message.DeliveryMethod}");
                        Shipment shipment = new Shipment(context.Message.CorrelationId.ToString(), false, context.Message.BookID, context.Message.BookQuantity);
                        var shipments = _scope.ServiceProvider.GetRequiredService<ShippingContext>();
                        shipments.Add(shipment);
                        shipments.SaveChanges();

                        context.Saga.BookID = context.Message.BookID;
                        context.Saga.BookQuantity = context.Message.BookQuantity;
                        context.Saga.CorrelationId = context.Message.CorrelationId;

                    })
                    .PublishAsync(context => context.Init<WarehouseDeliveryStart>(new { CorrelationId = context.Message.CorrelationId, BookID = context.Message.BookID, BookQuantity = context.Message.BookQuantity}))
                    .Schedule(ShippingWarehouseDeliveryConfirmationTimeout, context => context.Init<ShippingWarehouseDeliveryConfirmationTimeoutExpired>(new
                    {
                        ShippingId = context.Message.CorrelationId
                    }))
                    .TransitionTo(RequestSend)
                    );

                During(RequestSend,
                    When(WarehouseDeliveryConfirmationEvent)
                    .Unschedule(ShippingWarehouseDeliveryConfirmationTimeout)
                    .Then(context => {
                        var shipments = _scope.ServiceProvider.GetRequiredService<ShippingContext>();
                        Shipment shipment = shipments.ShipmentItems.SingleOrDefault(s => s.ID.Equals(context.Message.CorrelationId.ToString()));
                        if (shipment != null)
                        {
                            shipment.IsConfirmedByWarehouse = true;
                            shipments.SaveChanges();
                        }
                        Console.WriteLine($"Warehouse confirmed that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingShipmentSent>(new { CorrelationId = context.Message.CorrelationId }))
                    .Finalize(),
                    When(WarehouseDeliveryRejectionEvent)
                    .Then(context => {
                        Console.WriteLine($"Warehouse denied that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingShipmentNotSent>(new { CorrelationId = context.Message.CorrelationId }))
                    .Finalize(),
                    When(ShippingWarehouseDeliveryConfirmationTimeout.Received)
                    .Then(context => {
                        Console.WriteLine($"Warehouse did not confirm the availability in time");
                    })
                    .PublishAsync(context => context.Init<ShippingShipmentNotSent>(new { CorrelationId = context.Message.ShippingId }))
                    .Finalize()
                    );
                SetCompletedWhenFinalized();
            }

            public void Dispose()
            {
                _scope?.Dispose();
            }
        }

        public class DeliveryState : SagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public Guid? ShippingWarehouseDeliveryConfirmationTimeoutId { get; set; }
            public string CurrentState { get; set; }
            public string BookID { get; set; }
            public int BookQuantity { get; set; }
        }

        class ShippingConfirmationConsumer : IConsumer<ShippingConfirmation>
        {
            private ShippingContext _shippingContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public ShippingConfirmationConsumer(ShippingContext shippingContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _shippingContext = shippingContext;
            }
            public async Task Consume(ConsumeContext<ShippingConfirmation> context)
            {
                var deliveryPrice = context.Message.DeliveryPrice;
                var deliveryMethod = context.Message.DeliveryMethod;

                Method method = _shippingContext.MethodItems.SingleOrDefault(b => b.MethodValue.Equals(deliveryMethod));

                if (method == null)
                {
                    Console.WriteLine($"Method={deliveryMethod} was invalid for request {context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<ShippingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    Console.WriteLine($"Delivery information is correct for request {context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<ShippingConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
