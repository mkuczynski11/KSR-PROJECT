using Accounting.Models;
using Common;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Accounting.Configuration;
using Microsoft.Extensions.Configuration;

namespace Accounting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountingController : ControllerBase
    {
        private readonly ILogger<AccountingController> _logger;
        private readonly MongoClient _mongoClient;
        private readonly MongoDbConfiguration _mongoConf;
        public readonly IPublishEndpoint _publishEndpoint;

        public AccountingController(IPublishEndpoint publishEndpoint, MongoClient mongoClient, IConfiguration configuration, ILogger<AccountingController> logger)
        {
            _logger = logger;
            _mongoClient = mongoClient;
            _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet("invoices/{id}")]
        public ActionResult<InvoiceResponse> GetInvoice(string id)
        {
            _logger.LogInformation($"Invoice for order with ID:{id} requested");

            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Invoice>(_mongoConf.CollectionName.Invoices);

            Invoice invoice = collection.Find(o => o.ID.Equals(id)).SingleOrDefault();
            if (invoice == null) return NotFound();
            if (!invoice.IsPublic) return NoContent();

            return new InvoiceResponse { Text = invoice.Text, IsPaid = invoice.IsPaid, IsCanceled = invoice.IsCanceled };
        }

        [HttpPost("invoices/{id}/pay")]
        public ActionResult PayInvoice(string id)
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Invoice>(_mongoConf.CollectionName.Invoices);

            Invoice invoice = collection.Find(o => o.ID.Equals(id)).SingleOrDefault();
            if (invoice == null) return NotFound();
            if (invoice.IsCanceled) return BadRequest();
            if (!invoice.IsPublic) return BadRequest();

            if (!invoice.IsPaid)
            {
                invoice.IsPaid = true;
                invoice.Text += "\nPAID";
                collection.ReplaceOne(o => o.ID.Equals(id), invoice);

                _publishEndpoint.Publish<AccountingInvoicePaid>(new
                {
                    CorrelationId = invoice.ID
                });
            }

            return Ok();
        }
    }
}
