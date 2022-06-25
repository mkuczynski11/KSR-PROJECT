using Common;
using Contact.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Contact.Controllers
{
    [ApiController]
    [Route("contact/admin")]
    public class ContactAdminController : ControllerBase
    {
        private readonly OrderContext _orderContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public ContactAdminController(IPublishEndpoint publishEndpoint, OrderContext orderContext)
        {
            _orderContext = orderContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet("orders")]
        public IEnumerable<Order> GetOrders()
        {
            return _orderContext.OrderItems;
        }

        [HttpPost("orders/{id}/test1")]
        public ActionResult Test1Order(string id)
        {
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            _publishEndpoint.Publish<WarehouseConfirmationAccept>(new
            {
                CorrelationId = order.ID
            });
            _publishEndpoint.Publish<SalesConfirmationAccept>(new
            {
                CorrelationId = order.ID
            });
            _publishEndpoint.Publish<MarketingConfirmationAccept>(new
            {
                CorrelationId = order.ID
            });
            _publishEndpoint.Publish<ShippingConfirmationAccept>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
        }

        [HttpPost("orders/{id}/test2")]
        public ActionResult Test2Order(string id)
        {
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            _publishEndpoint.Publish<AccountingInvoicePaid>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
        }

        [HttpPost("orders/{id}/test3")]
        public ActionResult Test3Order(string id)
        {
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            _publishEndpoint.Publish<ShippingShipmentSent>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
        }
    }
}
