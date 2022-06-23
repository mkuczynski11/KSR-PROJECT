using Common;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Accounting.Models
{
    public class InvoiceSagaData : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public Guid? TimeoutId { get; set; }
        public string CurrentState { get; set; }
    }

    public class InvoiceSaga : MassTransitStateMachine<InvoiceSagaData>, IDisposable
    {
        private readonly IServiceScope _scope;

        public State AwaitingPublishing { get; private set; }
        public State AwaitingPayment { get; private set; }

        public Event<AccountingInvoiceStart> AccountingInvoiceStartEvent { get; private set; }
        public Event<AccountingInvoicePublish> AccountingInvoicePublishEvent { get; private set; }
        public Event<AccountingInvoiceCancel> AccountingInvoiceCancelEvent { get; private set; }
        public Event<AccountingInvoicePaid> AccountingInvoicePaidEvent { get; private set; }

        public InvoiceSaga(IServiceProvider services)
        {
            _scope = services.CreateScope();

            InstanceState(x => x.CurrentState);

            Initially(
                When(AccountingInvoiceStartEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}, preparing invoice.");

                    Invoice invoice = new Invoice(context.Message.CorrelationId.ToString(),
                        context.Message.BookID,
                        context.Message.BookName,
                        context.Message.BookQuantity,
                        context.Message.BookPrice,
                        context.Message.BookDiscount,
                        context.Message.DeliveryMethod,
                        context.Message.DeliveryPrice);

                    var invoices = _scope.ServiceProvider.GetRequiredService<InvoiceContext>();
                    invoices.InvoiceItems.Add(invoice);
                    invoices.SaveChanges();
                })
                .TransitionTo(AwaitingPublishing)
                );

            During(AwaitingPublishing,
                When(AccountingInvoicePublishEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}, publishing invoice.");

                    var invoices = _scope.ServiceProvider.GetRequiredService<InvoiceContext>();
                    Invoice invoice = invoices.InvoiceItems
                        .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (invoice != null)
                    {
                        invoice.IsPublic = true;
                        invoices.InvoiceItems.Update(invoice);
                        invoices.SaveChanges();
                    }
                })
                .TransitionTo(AwaitingPayment),
                When(AccountingInvoiceCancelEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}, publishing invoice canceled.");

                    var invoices = _scope.ServiceProvider.GetRequiredService<InvoiceContext>();
                    Invoice invoice = invoices.InvoiceItems
                        .SingleOrDefault(o => o.ID.Equals(context.Message.CorrelationId.ToString()));
                    if (invoice != null)
                    {
                        invoice.IsPublic = false;
                        invoices.InvoiceItems.Update(invoice);
                        invoices.SaveChanges();
                    }
                })
                .Finalize()
                );

            During(AwaitingPayment,
                When(AccountingInvoicePaidEvent)
                .Then(context =>
                {
                    Console.WriteLine($"Order ID={context.Message.CorrelationId}, invoice paid.");
                })
                .Finalize()
                //TODO: Add timeout for payment and send AccountingInvoiceNotPaid message
                );
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
