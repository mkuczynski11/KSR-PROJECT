using Common;
using Contact.Configuration;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Contact.Models
{
    

    public class OrderSagaData : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid? TimeoutId { get; set; }
        public string CurrentState { get; set; }
        public string DeliveryMethod { get; set; }
        public double DeliveryPrice { get; set; }
        public string BookID { get; set; }
        public string BookName { get; set; }
        public double BookPrice { get; set; }
        public double BookDiscount { get; set; }
        public int BookQuantity { get; set; }
        public bool ClientResponded { get; set; }
        public bool WarehouseResponded { get; set; }
        public bool SalesResponded { get; set; }
        public bool MarketingResponded { get; set; }
        public bool ShippingResponded { get; set; }

        public bool AllResponded()
        {
            return ClientResponded && WarehouseResponded && SalesResponded &&
                MarketingResponded && ShippingResponded;
        }
    }

    public class OrderSaga : MassTransitStateMachine<OrderSagaData>, IDisposable
    {
        private readonly ILogger<OrderSaga> _logger;
        private readonly IServiceScope _scope;

        public State AwaitingClientConfirmation { get; private set; }
        public State AwaitingServicesConfirmation { get; private set; }
        public State AwaitingPayment { get; private set; }
        public State AwaitingShipment { get; private set; }

        public Event<OrderStart> OrderStartEvent { get; private set; }

        public Event<ClientConfirmationAccept> ClientConfirmationAcceptEvent { get; private set; }
        public Event<WarehouseConfirmationAccept> WarehouseConfirmationAcceptEvent { get; private set; }
        public Event<SalesConfirmationAccept> SalesConfirmationAcceptEvent { get; private set; }
        public Event<MarketingConfirmationAccept> MarketingConfirmationAcceptEvent { get; private set; }
        public Event<ShippingConfirmationAccept> ShippingConfirmationAcceptEvent { get; private set; }

        public Event<ContactConfirmationConfirmedByAllParties> ContactConfirmationConfirmedByAllPartiesEvent { get; private set; }

        public Event<ClientConfirmationRefuse> ClientConfirmationRefuseEvent { get; private set; }
        public Event<WarehouseConfirmationRefuse> WarehouseConfirmationRefuseEvent { get; private set; }
        public Event<SalesConfirmationRefuse> SalesConfirmationRefuseEvent { get; private set; }
        public Event<MarketingConfirmationRefuse> MarketingConfirmationRefuseEvent { get; private set; }
        public Event<ShippingConfirmationRefuse> ShippingConfirmationRefuseEvent { get; private set; }

        public Event<ContactConfirmationRefusedByAtLeastOneParty> ContactConfirmationRefusedByAtLeastOnePartyEvent { get; private set; }

        public Event<AccountingInvoicePaid> AccountingInvoicePaidEvent { get; private set; }
        public Event<AccountingInvoiceNotPaid> AccountingInvoiceNotPaidEvent { get; private set; }

        public Event<ShippingShipmentSent> ShippingShipmentSentEvent { get; private set; }
        public Event<ShippingShipmentNotSent> ShippingShipmentNotSentEvent { get; private set; }

        public Schedule<OrderSagaData, ContactOrderClientConfirmationTimeoutExpired> ContactOrderClientConfirmationTimeout { get; private set; }
        public Schedule<OrderSagaData, ContactOrderServicesConfirmationTimeoutExpired> ContactOrderServicesConfirmationTimeout { get; private set; }
        public Schedule<OrderSagaData, ContactOrderPaymentTimeoutExpired> ContactOrderPaymentTimeout { get; private set; }
        public Schedule<OrderSagaData, ContactShipmentTimeoutExpired> ContactShipmentTimeout { get; private set; }

        public OrderSaga(IServiceProvider services, ILogger<OrderSaga> logger, IConfiguration configuration)
        {
            _logger = logger;
            _scope = services.CreateScope();

            var sagaConfiguration = configuration.GetSection("OrderSaga").Get<OrderSagaConfiguration>();
            var mongoDbConfiguration = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

            InstanceState(x => x.CurrentState);

            Schedule(() => ContactOrderClientConfirmationTimeout, instance => instance.TimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(sagaConfiguration.ClientConfirmationTimeoutSeconds);
                s.Received = r => r.CorrelateById(context => context.Message.OrderId);
            });
            Schedule(() => ContactOrderServicesConfirmationTimeout, instance => instance.TimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(sagaConfiguration.ServicesConfirmationTimeoutSeconds);
                s.Received = r => r.CorrelateById(context => context.Message.OrderId);
            });
            Schedule(() => ContactOrderPaymentTimeout, instance => instance.TimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(sagaConfiguration.PaymentTimeoutSeconds);
                s.Received = r => r.CorrelateById(context => context.Message.OrderId);
            });
            Schedule(() => ContactShipmentTimeout, instance => instance.TimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(sagaConfiguration.ShipmentTimeoutSeconds);
                s.Received = r => r.CorrelateById(context => context.Message.OrderId);
            });

            Initially(
                When(OrderStartEvent)
                .Then(context =>
                {
                    _logger.LogInformation($"Started new order saga: ID={context.Message.CorrelationId}, " +
                        $"sending confirmation requests to all parties.");

                    context.Saga.DeliveryMethod = context.Message.DeliveryMethod;
                    context.Saga.DeliveryPrice = context.Message.DeliveryPrice;
                    context.Saga.BookQuantity = context.Message.BookQuantity;
                    context.Saga.BookID = context.Message.BookID;
                    context.Saga.BookName = context.Message.BookName;
                    context.Saga.BookPrice = context.Message.BookPrice;
                    context.Saga.BookDiscount = context.Message.BookDiscount;
                    context.Saga.ClientResponded = false;
                    context.Saga.WarehouseResponded = false;
                    context.Saga.SalesResponded = false;
                    context.Saga.MarketingResponded = false;
                    context.Saga.ShippingResponded = false;
                })
                .Schedule(ContactOrderClientConfirmationTimeout, context => context.Init<ContactOrderClientConfirmationTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingClientConfirmation)
                );

            During(AwaitingClientConfirmation,

                When(ClientConfirmationAcceptEvent)
                .Unschedule(ContactOrderClientConfirmationTimeout)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                        $"received client confirmation.");

                    context.Saga.ClientResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByClient = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }
                })
                .PublishAsync(context => context.Init<WarehouseConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Saga.BookID,
                    BookName = context.Saga.BookName,
                    BookQuantity = context.Saga.BookQuantity
                }))
                .PublishAsync(context => context.Init<SalesConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Saga.BookID,
                    BookPrice = context.Saga.BookPrice
                }))
                .PublishAsync(context => context.Init<MarketingConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Saga.BookID,
                    BookDiscount = context.Saga.BookDiscount
                }))
                .PublishAsync(context => context.Init<ShippingConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    DeliveryMethod = context.Saga.DeliveryMethod,
                    DeliveryPrice = context.Saga.DeliveryPrice
                }))
                .PublishAsync(context => context.Init<AccountingInvoiceStart>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Saga.BookID,
                    BookName = context.Saga.BookName,
                    BookQuantity = context.Saga.BookQuantity,
                    BookPrice = context.Saga.BookPrice,
                    BookDiscount = context.Saga.BookDiscount,
                    DeliveryMethod = context.Saga.DeliveryMethod,
                    DeliveryPrice = context.Saga.DeliveryPrice
                }))
                .Schedule(ContactOrderServicesConfirmationTimeout, context => context.Init<ContactOrderServicesConfirmationTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingServicesConfirmation),

                When(ClientConfirmationRefuseEvent)
                .Unschedule(ContactOrderClientConfirmationTimeout)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by client.");

                    context.Saga.ClientResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByClient = false;
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }
                })
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),

                When(ContactOrderClientConfirmationTimeout.Received)
                .Then(context =>
                {
                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.OrderId.ToString())).SingleOrDefault();

                    string message = $"Order ID={context.Message.OrderId}: " +
                        $"client confirmation time expired.";

                    if (order != null)
                    {
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.OrderId.ToString()), order);

                        message += "\n" + order;
                    }

                    Console.WriteLine(message);
                })
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.OrderId
                }))
                .Finalize()
                );


            During(AwaitingServicesConfirmation,

                When(WarehouseConfirmationAcceptEvent)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"received warehouse confirmation.");

                    context.Saga.WarehouseResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            if (order.isConfirmed())
                            {
                                context.Publish<AccountingInvoicePublish>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                                context.Publish<ContactConfirmationConfirmedByAllParties>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                            else
                            {
                                context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                        }
                    }
                }),
                When(SalesConfirmationAcceptEvent)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"received sales confirmation.");

                    context.Saga.SalesResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedBySales = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            if (order.isConfirmed())
                            {
                                context.Publish<AccountingInvoicePublish>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                                context.Publish<ContactConfirmationConfirmedByAllParties>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                            else
                            {
                                context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                        }
                    }
                }),
                When(MarketingConfirmationAcceptEvent)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"received marketing confirmation.");

                    context.Saga.MarketingResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            if (order.isConfirmed())
                            {
                                context.Publish<AccountingInvoicePublish>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                                context.Publish<ContactConfirmationConfirmedByAllParties>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                            else
                            {
                                context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                        }
                    }
                }),
                When(ShippingConfirmationAcceptEvent)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"received shipping confirmation.");

                    context.Saga.ShippingResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            if (order.isConfirmed())
                            {
                                context.Publish<AccountingInvoicePublish>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                                context.Publish<ContactConfirmationConfirmedByAllParties>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                            else
                            {
                                context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                                {
                                    CorrelationId = context.Message.CorrelationId
                                });
                            }
                        }
                    }
                }),
                When(ContactConfirmationConfirmedByAllPartiesEvent)
                .Unschedule(ContactOrderServicesConfirmationTimeout)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"confirmed by all parties.");
                })
                .Schedule(ContactOrderPaymentTimeout, context => context.Init<ContactOrderPaymentTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingPayment),

                When(ContactOrderServicesConfirmationTimeout.Received)
                .Then(context => 
                {
                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.OrderId.ToString())).SingleOrDefault();

                    string message = $"Order ID={context.Message.OrderId}: " +
                        $"services confirmation time expired.";

                    if (order != null)
                    {
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.OrderId.ToString()), order);

                        message += "\n" + order;
                    }

                    _logger.LogError(message);
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.OrderId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.OrderId
                }))
                .Finalize(),

                When(WarehouseConfirmationRefuseEvent)
                .Then(context =>
                {
                    _logger.LogError($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by warehouse.");

                    context.Saga.WarehouseResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = false;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                            {
                                CorrelationId = context.Message.CorrelationId
                            });
                        }
                    }
                }),
                When(SalesConfirmationRefuseEvent)
                .Then(context =>
                {
                    _logger.LogError($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by sales.");

                    context.Saga.SalesResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedBySales = false;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                            {
                                CorrelationId = context.Message.CorrelationId
                            });
                        }
                    }
                }),
                When(MarketingConfirmationRefuseEvent)
                .Then(context =>
                {
                    _logger.LogError($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by marketing.");

                    context.Saga.MarketingResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = false;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                            {
                                CorrelationId = context.Message.CorrelationId
                            });
                        }
                    }
                }),
                When(ShippingConfirmationRefuseEvent)
                .Then(context =>
                {
                    _logger.LogError($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by shipping.");

                    context.Saga.ShippingResponded = true;

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = false;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        if (context.Saga.AllResponded())
                        {
                            context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                            {
                                CorrelationId = context.Message.CorrelationId
                            });
                        }
                    }
                }),
                When(ContactConfirmationRefusedByAtLeastOnePartyEvent)
                .Unschedule(ContactOrderServicesConfirmationTimeout)
                .Then(context =>
                {
                    string message = $"Order ID={context.Message.CorrelationId}: " +
                            $"refused by at least one party.";

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);

                        message += "\n" + order;
                    }

                    Console.WriteLine(message);
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize()
                );

            During(AwaitingPayment,
                When(AccountingInvoicePaidEvent)
                .Unschedule(ContactOrderPaymentTimeout)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"has been paid by client.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsPaid = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }

                    context.Publish<ShippingShipmentStart>(new
                    {
                        CorrelationId = context.Message.CorrelationId,
                        DeliveryMethod = context.Saga.DeliveryMethod,
                        DeliveryPrice = context.Saga.DeliveryPrice,
                        BookID = context.Saga.BookID,
                        BookQuantity = context.Saga.BookQuantity
                    });
                })
                .Schedule(ContactShipmentTimeout, context => context.Init<ContactShipmentTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingShipment),
                When(AccountingInvoiceNotPaidEvent)
                .Unschedule(ContactOrderPaymentTimeout)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"has not been paid by client.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsPaid = false;
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }
                })
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),
                When(ContactOrderPaymentTimeout.Received)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.OrderId}: " +
                            $"payment time expired.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.OrderId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsPaid = false;
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.OrderId.ToString()), order);
                    }
                })
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.OrderId
                }))
                .Finalize()
                );

            During(AwaitingShipment,
                When(ShippingShipmentSentEvent)
                .Unschedule(ContactShipmentTimeout)
                .Then(context =>
                {
                    _logger.LogInformation($"Order ID={context.Message.CorrelationId}: " +
                            $"has been shipped to client.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsShipped = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }
                })
                .Finalize(),

                When(ShippingShipmentNotSentEvent)
                .Unschedule(ContactShipmentTimeout)
                .Then(context =>
                {
                    _logger.LogError($"Order ID={context.Message.CorrelationId}: " +
                            $"could not be shipped to client.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsShipped = false;
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.CorrelationId.ToString()), order);
                    }
                })
                .Finalize(),
                When(ContactShipmentTimeout.Received)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.OrderId}: " +
                            $"shipment time expired.");

                    var client = _scope.ServiceProvider.GetRequiredService<MongoClient>();
                    var collection = client.GetDatabase(mongoDbConfiguration.DatabaseName)
                        .GetCollection<Order>(mongoDbConfiguration.CollectionName.Orders);

                    Order order = collection.Find(o => o.ID.Equals(context.Message.OrderId.ToString())).SingleOrDefault();
                    if (order != null)
                    {
                        order.IsShipped = false;
                        order.IsCanceled = true;
                        collection.ReplaceOne(o => o.ID.Equals(context.Message.OrderId.ToString()), order);
                    }
                })
                .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
