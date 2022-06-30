using Common;
using Contact.Configuration;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Models
{
    public interface ContactOrderConfirmationTimeoutExpired
    {
        Guid OrderId { get; }
    }
    public interface ContactOrderPaymentTimeoutExpired
    {
        Guid OrderId { get; }
    }
    public interface ContactShipmentTimeoutExpired
    {
        Guid OrderId { get; }
    }

    public interface ContactConfirmationConfirmedByAllParties : CorrelatedBy<Guid> { }
    public interface ContactConfirmationRefusedByAtLeastOneParty : CorrelatedBy<Guid> { }

    public class OrderSagaData : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? TimeoutId { get; set; }
        public string CurrentState { get; set; }
        public string DeliveryMethod { get; set; }
        public double DeliveryPrice { get; set; }
        public string BookID { get; set; }
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
        private readonly IServiceScope _scope;

        public State AwaitingConfirmation { get; private set; }
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

        public Schedule<OrderSagaData, ContactOrderConfirmationTimeoutExpired> ContactOrderConfirmationTimeout { get; private set; }
        public Schedule<OrderSagaData, ContactOrderPaymentTimeoutExpired> ContactOrderPaymentTimeout { get; private set; }
        public Schedule<OrderSagaData, ContactShipmentTimeoutExpired> ContactShipmentTimeout { get; private set; }

        public OrderSaga(IServiceProvider services, IConfiguration configuration)
        {
            _scope = services.CreateScope();

            var sagaConfiguration = configuration.GetSection("OrderSaga").Get<OrderSagaConfiguration>();

            InstanceState(x => x.CurrentState);

            Schedule(() => ContactOrderConfirmationTimeout, instance => instance.TimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(sagaConfiguration.ConfirmationTimeoutSeconds);
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
                    Console.WriteLine($"Started new order saga: ID={context.Message.CorrelationId}, " +
                        $"sending confirmation requests to all parties.");

                    context.Saga.DeliveryMethod = context.Message.DeliveryMethod;
                    context.Saga.DeliveryPrice = context.Message.DeliveryPrice;
                    context.Saga.BookQuantity = context.Message.BookQuantity;
                    context.Saga.BookID = context.Message.BookID;
                    context.Saga.ClientResponded = false;
                    context.Saga.WarehouseResponded = false;
                    context.Saga.SalesResponded = false;
                    context.Saga.MarketingResponded = false;
                    context.Saga.ShippingResponded = false;
                })
                .PublishAsync(context => context.Init<WarehouseConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Message.BookID,
                    BookName = context.Message.BookName,
                    BookQuantity = context.Message.BookQuantity
                }))
                .PublishAsync(context => context.Init<SalesConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Message.BookID,
                    BookPrice = context.Message.BookPrice
                }))
                .PublishAsync(context => context.Init<MarketingConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Message.BookID,
                    BookDiscount = context.Message.BookDiscount
                }))
                .PublishAsync(context => context.Init<ShippingConfirmation>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    DeliveryMethod = context.Message.DeliveryMethod,
                    DeliveryPrice = context.Message.DeliveryPrice
                }))
                .PublishAsync(context => context.Init<AccountingInvoiceStart>(new
                {
                    CorrelationId = context.Message.CorrelationId,
                    BookID = context.Message.BookID,
                    BookName = context.Message.BookName,
                    BookQuantity = context.Message.BookQuantity,
                    BookPrice = context.Message.BookPrice,
                    BookDiscount = context.Message.BookDiscount,
                    DeliveryMethod = context.Message.DeliveryMethod,
                    DeliveryPrice = context.Message.DeliveryPrice
                }))
                .Schedule(ContactOrderConfirmationTimeout, context => context.Init<ContactOrderConfirmationTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingConfirmation)
                );


            During(AwaitingConfirmation,

                When(ClientConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                        $"received client confirmation.");

                    context.Saga.ClientResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByClient = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                When(WarehouseConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received warehouse confirmation.");

                    context.Saga.WarehouseResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received sales confirmation.");

                    context.Saga.SalesResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedBySales = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received marketing confirmation.");

                    context.Saga.MarketingResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received shipping confirmation.");

                    context.Saga.ShippingResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                .Unschedule(ContactOrderConfirmationTimeout)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"confirmed by all parties.");
                })
                .Schedule(ContactOrderPaymentTimeout, context => context.Init<ContactOrderPaymentTimeoutExpired>(new
                {
                    OrderId = context.Message.CorrelationId
                }))
                .TransitionTo(AwaitingPayment),

                When(ContactOrderConfirmationTimeout.Received)
                .Then(context => 
                {
                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.OrderId.ToString()));

                    string message = $"Order ID={context.Message.OrderId}: " +
                        $"confirmation time expired.";

                    if (order != null)
                    {
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        message += "\n" + order;
                    }

                    Console.WriteLine(message);
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

                When(ClientConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by client.");

                    context.Saga.ClientResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByClient = false;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (context.Saga.AllResponded())
                        {
                            context.Publish<ContactConfirmationRefusedByAtLeastOneParty>(new
                            {
                                CorrelationId = context.Message.CorrelationId
                            });
                        }
                    }
                }),
                When(WarehouseConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by warehouse.");

                    context.Saga.WarehouseResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = false;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by sales.");

                    context.Saga.SalesResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedBySales = false;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by marketing.");

                    context.Saga.MarketingResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = false;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by shipping.");

                    context.Saga.ShippingResponded = true;

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = false;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                .Unschedule(ContactOrderConfirmationTimeout)
                .Then(context =>
                {
                    string message = $"Order ID={context.Message.CorrelationId}: " +
                            $"refused by at least one party.";

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"has been paid by client.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsPaid = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"has not been paid by client.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsPaid = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
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

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.OrderId.ToString()));
                    if (order != null)
                    {
                        order.IsPaid = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
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
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"has been shipped to client.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsShipped = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }
                })
                .Finalize(),

                When(ShippingShipmentNotSentEvent)
                .Unschedule(ContactShipmentTimeout)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"could not be shipped to client.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsShipped = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }
                })
                .Finalize(),
                When(ContactShipmentTimeout.Received)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.OrderId}: " +
                            $"shipment time expired.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.OrderId.ToString()));
                    if (order != null)
                    {
                        order.IsShipped = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
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
