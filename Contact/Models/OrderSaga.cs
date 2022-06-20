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
        public State AwaitingConfirmation { get; private set; }
        public State AwaitingPayment { get; private set; }
        public State AwaitingShipment { get; private set; }

        public Event<NewOrderStart> newOrderStart { get; private set; }

        public OrderSaga()
        {
            InstanceState(x => x.CurrentState);

            Initially(
                When(newOrderStart)
                .Then(context =>
                {
                    Console.WriteLine($"Started new order saga: ID={context.Message.CorrelationId}");
                })
                .Finalize()
                );
        }
    }
}
