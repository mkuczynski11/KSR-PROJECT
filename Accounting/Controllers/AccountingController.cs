using Accounting.Models;
using Common;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Accounting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountingController : ControllerBase
    {
        private readonly InvoiceContext _invoiceContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public AccountingController(IPublishEndpoint publishEndpoint, InvoiceContext invoiceContext)
        {
            _invoiceContext = invoiceContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet("invoices/{id}")]
        public ActionResult<InvoiceResponse> GetInvoice(string id)
        {
            Console.WriteLine($"Invoice for order with ID:{id} requested");
            Invoice invoice = _invoiceContext.InvoiceItems.SingleOrDefault(o => o.ID.Equals(id));
            if (invoice == null) return NotFound();
            if (!invoice.IsPublic) return NotFound();

            return new InvoiceResponse { Text = invoice.Text, IsPaid = invoice.IsPaid };
        }

        [HttpPost("invoices/{id}/pay")]
        public ActionResult PayInvoice(string id)
        {
            Invoice invoice = _invoiceContext.InvoiceItems.SingleOrDefault(o => o.ID.Equals(id));
            if (invoice == null) return NotFound();
            if (!invoice.IsPublic) return NotFound();

            if (!invoice.IsPaid)
            {
                invoice.IsPaid = true;
                invoice.Text += "\nPAID";
                _invoiceContext.Update(invoice);
                _invoiceContext.SaveChanges();

                _publishEndpoint.Publish<AccountingInvoicePaid>(new
                {
                    CorrelationId = invoice.ID
                });
            }

            return Ok();
        }
    }
}
