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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var repo = new InMemorySagaRepository<DeliveryState>();
            var machine = new DeliveryStateMachine();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<DeliveryCheckConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("shipping-saga-queue", ep =>
                    {
                        ep.StateMachineSaga(machine, repo);
                    });

                    cfg.ReceiveEndpoint("shipping-delivery-confirmation-request-event", ep =>
                    {
                        ep.ConfigureConsumer<DeliveryCheckConsumer>(context);
                    });

                });
            });

            services.AddDbContext<ShippingContext>(opt => opt.UseInMemoryDatabase("ShippingInfo"));
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            var price = new Price(10.0);
            context.PriceItems.Add(price);

            var dpdMethod = new Method("DPD");
            context.MethodItems.Add(dpdMethod);

            var ppMethod = new Method("Poczta polska");
            context.MethodItems.Add(ppMethod);

            var ooMethod = new Method("Odbiór osobisty");
            context.MethodItems.Add(ooMethod);

            context.SaveChanges();
        }

        public class DeliveryStateMachine : MassTransitStateMachine<DeliveryState> 
        {
            public State RequestSend { get; private set; }
            public State ReadyToDeliver { get; private set; }
            public State WillNotDeliver { get; private set; }

            public Event<ShippingRequest> ShippingRequestEvent { get; private set; }
            public Event<WarehouseDeliveryConfirmation> WarehouseDeliveryConfirmationEvent { get; private set; }
            public Event<WarehouseDeliveryRejection> WarehouseDeliveryRejectionEvent { get; private set; }
            public DeliveryStateMachine()
            {
                InstanceState(x => x.CurrentState);

                Initially(
                    When(ShippingRequestEvent)
                    .Then(context => {
                        Console.WriteLine($"Got shipping request for book={context.Message.ID}, quantity={context.Message.quantity}");
                    })
                    .PublishAsync(context => context.Init<WarehouseDeliveryRequest>(new { ID = context.Message.ID, quantity = context.Message.quantity}))
                    .TransitionTo(RequestSend)
                    );

                During(RequestSend,
                    When(WarehouseDeliveryConfirmationEvent)
                    .Then(context => {
                        Console.WriteLine($"Warehouse confirmed that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingConfirmed>(new { }))
                    .Finalize(),
                    When(WarehouseDeliveryRejectionEvent)
                    .Then(context => {
                        Console.WriteLine($"Warehouse denied that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingRejected>(new { }))
                    .Finalize()
                    );
                SetCompletedWhenFinalized();
            }
        }

        public class DeliveryState : SagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
            public string CurrentState { get; set; }
            public string bookID { get; set; }
            public int bookQuantity { get; set; }
        }

        class DeliveryCheckConsumer : IConsumer<DeliveryCheck>
        {
            private ShippingContext _shippingContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public DeliveryCheckConsumer(ShippingContext shippingContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _shippingContext = shippingContext;
            }
            public async Task Consume(ConsumeContext<DeliveryCheck> context)
            {
                var deliveryPrice = context.Message.price;
                var deliveryMethod = context.Message.method;

                Price price = _shippingContext.PriceItems.SingleOrDefault(b => b.price.Equals(deliveryPrice));
                Method method = _shippingContext.MethodItems.SingleOrDefault(b => b.method.Equals(deliveryMethod));

                if (price != null && method != null)
                {
                    Console.WriteLine($"Delivery information is valid.");
                    await _publishEndpoint.Publish<BookQuantityConfirmation>(new { });
                }
                else
                {
                    Console.WriteLine($"Delivery information is invalid.");
                    await _publishEndpoint.Publish<BookQuantityRejection>(new { });
                }
            }
        }
    }
}
