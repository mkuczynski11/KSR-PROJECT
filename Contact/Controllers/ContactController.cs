using Common;
using Contact.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly OrderContext _orderContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public ContactController(IPublishEndpoint publishEndpoint, OrderContext orderContext)
        {
            _orderContext = orderContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("orders/create")]
        public ActionResult<OrderCreateResponse> StartOrder([FromBody] OrderCreateRequest request)
        {
            Console.WriteLine($"New Order: data={request}");
            Guid ID = Guid.NewGuid();

            Order order = new Order(ID.ToString());

            _orderContext.OrderItems.Add(order);
            _orderContext.SaveChanges();

            _publishEndpoint.Publish<OrderStart>(new
            {
                CorrelationId = ID,
                BookID = request.BookID,
                BookName = request.BookName,
                BookQuantity = request.BookQuantity,
                BookPrice = request.BookPrice,
                BookDiscount = request.BookDiscount,
                DeliveryMethod = request.DeliveryMethod,
                DeliveryPrice = request.DeliveryPrice,
            });

            return new OrderCreateResponse { OrderID = ID.ToString() };
        }

        [HttpPost("orders/{id}/confirm")]
        public ActionResult ConfirmOrder(string id)
        {
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            _publishEndpoint.Publish<ClientConfirmationAccept>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
        }

        [HttpPost("orders/{id}/cancel")]
        public ActionResult CancelOrder(string id)
        {
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            _publishEndpoint.Publish<ClientConfirmationRefuse>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
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

        [HttpGet("orders/{id}/status")]
        public ActionResult<OrderStatusResponse> GetStatus(string id)
        {
            Console.WriteLine($"Order status with ID:{id} requested");
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            if (order.IsCanceled) return new OrderStatusResponse { Status = "Canceled" };

            if (order.isConfirmed())
            {
                if (order.IsPaid)
                {
                    if (order.IsShipped)
                    {
                        return new OrderStatusResponse { Status = "Shipped to customer" };
                    }
                    else
                    {
                        return new OrderStatusResponse { Status = "Awaiting shipment" };
                    }
                }
                else
                {
                    return new OrderStatusResponse { Status = "Awaiting payment" };
                }
            }
            else
            {
                return new OrderStatusResponse { Status = "Awaiting confirmation" };
            }
        }
    }
}
