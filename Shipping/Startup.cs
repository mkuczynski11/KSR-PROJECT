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
using Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Collections.Generic;

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
            var mongoDbConfiguration = Configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

            services.AddMassTransit(x =>
            {
                x.AddSagaStateMachine<DeliveryStateMachine, DeliveryState>()
                    .MongoDbRepository(r =>
                    {
                        r.Connection = mongoDbConfiguration.Connection;
                        r.DatabaseName = mongoDbConfiguration.DatabaseName;
                        r.CollectionName = mongoDbConfiguration.CollectionName.Saga;
                    });
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

            services.AddHealthChecks()
                .AddMongoDb(mongoDbConfiguration.Connection)
                .AddRabbitMQ(rabbitConnectionString: rabbitConfiguration.ConnStr);
            services.AddSingleton(new MongoClient(mongoDbConfiguration.Connection));
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MongoClient mongoClient, IConfiguration configuration)
        {
            AddShippingData(mongoClient, configuration);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/health");
            });
        }

        private static void AddShippingData(MongoClient client, IConfiguration configuration)
        {
            var mongoDbConfiguration = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

            var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                .GetCollection<Method>(mongoDbConfiguration.CollectionName.Methods);

            var methods = new List<Method>();

            methods.Add(new Method("DPD", 10.0));
            methods.Add(new Method("Poczta polska", 13.0));
            methods.Add(new Method("Odbiór osobisty", 0.0));

            collection.InsertMany(methods);
        }

        public class DeliveryStateMachine : MassTransitStateMachine<DeliveryState>, IDisposable
        {
            private readonly IServiceScope _scope;
            private readonly ILogger<DeliveryStateMachine> _logger;

            public State RequestSend { get; private set; }
            public State ReadyToDeliver { get; private set; }
            public State WillNotDeliver { get; private set; }

            public Event<ShippingShipmentStart> ShippingShipmentStartEvent { get; private set; }
            public Event<WarehouseDeliveryStartConfirmation> WarehouseDeliveryConfirmationEvent { get; private set; }
            public Event<WarehouseDeliveryStartRejection> WarehouseDeliveryRejectionEvent { get; private set; }

            public Schedule<DeliveryState, ShippingWarehouseDeliveryConfirmationTimeoutExpired> ShippingWarehouseDeliveryConfirmationTimeout { get; private set; }

            public DeliveryStateMachine(IServiceProvider services, IConfiguration configuration, ILogger<DeliveryStateMachine> logger)
            {
                _logger = logger;
                _scope = services.CreateScope();

                var sagaConfiguration = configuration.GetSection("ShippingSaga").Get<ShippingSagaConfiguration>();
                var mongoDbConfiguration = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

                InstanceState(x => x.CurrentState);

                Schedule(() => ShippingWarehouseDeliveryConfirmationTimeout, instance => instance.ShippingWarehouseDeliveryConfirmationTimeoutId, s =>
                {
                    s.Delay = TimeSpan.FromSeconds(sagaConfiguration.WarehouseDeliveryConfirmationTimeoutSeconds);
                    s.Received = r => r.CorrelateById(context => context.Message.ShippingId);
                });

                Initially(
                    When(ShippingShipmentStartEvent)
                    .Then(context => {
                        _logger.LogInformation($"Got shipping request for book={context.Message.BookID}, quantity={context.Message.BookQuantity}, price={context.Message.DeliveryPrice}, method={context.Message.DeliveryMethod}");
                        Shipment shipment = new Shipment(context.Message.CorrelationId.ToString(), false, context.Message.BookID, context.Message.BookQuantity);

                        var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                        var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                            .GetCollection<Shipment>(mongoDbConfiguration.CollectionName.Shipments);

                        collection.InsertOne(shipment);

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
                        var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                        var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                            .GetCollection<Shipment>(mongoDbConfiguration.CollectionName.Shipments);

                        Shipment shipment = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                        if (shipment != null)
                        {
                            shipment.IsConfirmedByWarehouse = true;
                            collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), shipment);
                        }
                        _logger.LogInformation($"Warehouse confirmed that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingShipmentSent>(new { CorrelationId = context.Message.CorrelationId }))
                    .Finalize(),
                    When(WarehouseDeliveryRejectionEvent)
                    .Then(context => {
                        _logger.LogError($"Warehouse denied that this book is available");
                    })
                    .PublishAsync(context => context.Init<ShippingShipmentNotSent>(new { CorrelationId = context.Message.CorrelationId }))
                    .Finalize(),
                    When(ShippingWarehouseDeliveryConfirmationTimeout.Received)
                    .Then(context => {
                        _logger.LogError($"Warehouse did not confirm the availability in time");
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

        public class DeliveryState : SagaStateMachineInstance, ISagaVersion
        {
            public Guid CorrelationId { get; set; }
            public int Version { get; set; }
            public Guid? ShippingWarehouseDeliveryConfirmationTimeoutId { get; set; }
            public string CurrentState { get; set; }
            public string BookID { get; set; }
            public int BookQuantity { get; set; }
        }

        class ShippingConfirmationConsumer : IConsumer<ShippingConfirmation>
        {
            private readonly ILogger<ShippingConfirmationConsumer> _logger;
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public ShippingConfirmationConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<ShippingConfirmationConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<ShippingConfirmation> context)
            {
                var deliveryPrice = context.Message.DeliveryPrice;
                var deliveryMethod = context.Message.DeliveryMethod;

                var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Method>(_mongoConf.CollectionName.Methods);

                Method method = collection.Find(o => o.MethodValue.Equals(deliveryMethod)).SingleOrDefault();

                if (method == null)
                {
                    _logger.LogError($"Method={deliveryMethod} was invalid for request {context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<ShippingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    _logger.LogInformation($"Delivery information is correct for request {context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<ShippingConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
