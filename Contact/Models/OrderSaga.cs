using Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Models
{
    public class OrderSagaData : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? TimeoutId { get; set; }
        public string CurrentState { get; set; }
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

        public Event<ClientConfirmationRefuse> ClientConfirmationRefuseEvent { get; private set; }
        public Event<WarehouseConfirmationRefuse> WarehouseConfirmationRefuseEvent { get; private set; }
        public Event<SalesConfirmationRefuse> SalesConfirmationRefuseEvent { get; private set; }
        public Event<MarketingConfirmationRefuse> MarketingConfirmationRefuseEvent { get; private set; }
        public Event<ShippingConfirmationRefuse> ShippingConfirmationRefuseEvent { get; private set; }

        public OrderSaga(IServiceProvider services)
        {
            _scope = services.CreateScope();

            InstanceState(x => x.CurrentState);

            Initially(
                When(OrderStartEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Started new order saga: ID={context.Message.CorrelationId}, " +
                        $"sending confirmation requests to all parties.");
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
                    if (orders != null)
                    {
                        order.IsConfirmedByClient = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (order.isConfirmed()) context.TransitionToState(AwaitingPayment);
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
                    if (orders != null)
                    {
                        order.IsConfirmedByWarehouse = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (order.isConfirmed()) context.TransitionToState(AwaitingPayment);
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
                    if (orders != null)
                    {
                        order.IsConfirmedBySales = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (order.isConfirmed()) context.TransitionToState(AwaitingPayment);
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
                    if (orders != null)
                    {
                        order.IsConfirmedByMarketing = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (order.isConfirmed()) context.TransitionToState(AwaitingPayment);
                    }
                }),
                When(ShippingConfirmationAcceptEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"received marketing confirmation.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (orders != null)
                    {
                        order.IsConfirmedByShipping = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();

                        if (order.isConfirmed()) context.TransitionToState(AwaitingPayment);
                    }
                }),

                When(ClientConfirmationRefuseEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}: " +
                            $"canceled by client.");

                    var orders = _scope.ServiceProvider.GetRequiredService<OrderContext>();
                    Order order = orders.OrderItems
                            .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (orders != null)
                    {
                        order.IsConfirmedByClient = false;
                        order.IsCanceled = true;
                        orders.OrderItems.Update(order);
                        orders.SaveChanges();
                    }
                })
                .Finalize()
                );

            //During(AwaitingPayment,

            //    );
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
