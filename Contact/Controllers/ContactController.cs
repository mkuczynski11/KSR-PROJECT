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

            _publishEndpoint.Publish<NewOrderStart>(new
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
            Console.WriteLine($"Order with ID:{id} confirmed");
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            order.IsConfirmedByClient = true;
            _orderContext.OrderItems.Update(order);
            _orderContext.SaveChanges();

            return Ok();
        }

        [HttpGet("orders/{id}/status")]
        public ActionResult<OrderStatusResponse> GetStatus(string id)
        {
            Console.WriteLine($"Order status with ID:{id} requested");
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

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
