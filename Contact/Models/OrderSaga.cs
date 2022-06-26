using Common;
using MassTransit;
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

    public interface ContactConfirmationConfirmedByAllParties : CorrelatedBy<Guid> { }

    public class OrderSagaData : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? ContactOrderConfirmationTimeoutId { get; set; }
        public string CurrentState { get; set; }
        public string DeliveryMethod { get; set; }
        public double DeliveryPrice { get; set; }
        public string BookID { get; set; }
        public int BookQuantity { get; set; }
    }

    public class OrderSaga : MassTransitStateMachine<OrderSagaData>, IDisposable
    {
        public const int ConfirmationTimeoutSeconds = 30;

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

        public Event<AccountingInvoicePaid> AccountingInvoicePaidEvent { get; private set; }
        public Event<AccountingInvoiceNotPaid> AccountingInvoiceNotPaidEvent { get; private set; }

        public Event<ShippingShipmentSent> ShippingShipmentSentEvent { get; private set; }
        public Event<ShippingShipmentNotSent> ShippingShipmentNotSentEvent { get; private set; }

        public Schedule<OrderSagaData, ContactOrderConfirmationTimeoutExpired> ContactOrderConfirmationTimeout { get; private set; }

        public OrderSaga(IServiceProvider services)
        {
            _scope = services.CreateScope();

            InstanceState(x => x.CurrentState);

            Schedule(() => ContactOrderConfirmationTimeout, instance => instance.ContactOrderConfirmationTimeoutId, s =>
            {
                s.Delay = TimeSpan.FromSeconds(ConfirmationTimeoutSeconds);
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

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByClient = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                            //context.TransitionToState(AwaitingPayment);
                        }
                    }
                }),
                When(WarehouseConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received warehouse confirmation.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                            //context.TransitionToState(AwaitingPayment);
                        }
                    }
                }),
                When(SalesConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received sales confirmation.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedBySales = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                            //context.TransitionToState(AwaitingPayment);
                        }
                    }
                }),
                When(MarketingConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received marketing confirmation.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                            //context.TransitionToState(AwaitingPayment);
                        }
                    }
                }),
                When(ShippingConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received shipping confirmation.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

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
                            //context.TransitionToState(AwaitingPayment);
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
                .TransitionTo(AwaitingPayment),

                When(ContactOrderConfirmationTimeout.Received)
                .Then(context => 
                {
                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.OrderId.ToString()));

                    string message = $"Order ID={context.Message.OrderId}: ";

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

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByClient = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }

                    //context.Publish<AccountingInvoiceCancel>(new
                    //{
                    //    CorrelationId = context.Message.CorrelationId
                    //});
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),
                When(WarehouseConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by warehouse.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByWarehouse = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }

                    //context.Publish<AccountingInvoiceCancel>(new
                    //{
                    //    CorrelationId = context.Message.CorrelationId
                    //});
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),
                When(SalesConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by sales.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedBySales = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }

                    //context.Publish<AccountingInvoiceCancel>(new
                    //{
                    //    CorrelationId = context.Message.CorrelationId
                    //});
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),
                When(MarketingConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by marketing.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByMarketing = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }

                    //context.Publish<AccountingInvoiceCancel>(new
                    //{
                    //    CorrelationId = context.Message.CorrelationId
                    //});
                })
                .PublishAsync(context => context.Init<AccountingInvoiceCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .PublishAsync(context => context.Init<OrderCancel>(new
                {
                    CorrelationId = context.Message.CorrelationId
                }))
                .Finalize(),
                When(ShippingConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by shipping.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (order != null)
                    {
                        order.IsConfirmedByShipping = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }

                    //context.Publish<AccountingInvoiceCancel>(new
                    //{
                    //    CorrelationId = context.Message.CorrelationId
                    //});
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
                .TransitionTo(AwaitingShipment),
                When(AccountingInvoiceNotPaidEvent)
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
                .Finalize()
                );

            During(AwaitingShipment,
                When(ShippingShipmentSentEvent)
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
                .Finalize()
                );
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
