using Common;
using MassTransit;
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

    public class OrderSaga : MassTransitStateMachine<OrderSagaData>
    {
        private readonly OrderContext _orderContext;

        public State AwaitingConfirmation { get; private set; }
        public State AwaitingPayment { get; private set; }
        public State AwaitingShipment { get; private set; }

        public Event<OrderStart> OrderStartEvent { get; private set; }
        public Event<ClientConfirmationAccept> ClientConfirmationAcceptEvent { get; private set; }

        public OrderSaga()
        {
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
                );
        }
    }
}
