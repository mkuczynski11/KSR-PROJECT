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
    [Route("contact/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly OrderContext _orderContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public ContactController(IPublishEndpoint publishEndpoint, OrderContext orderContext)
        {
            _orderContext = orderContext;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("order/create")]
        public void StartOrder()
        {
            //TODO: start order saga
        }

        [HttpPost("order/{id}/confirm")]
        public void ConfirmOrder()
        {
            //TODO: implement order confirmation logic
        }

        [HttpGet("order/{id}/status")]
        public ActionResult<OrderStatusResponse> GetStatus(string id)
        {
            Console.WriteLine($"Order status with ID:{id} requested");
            Order order = _orderContext.OrderItems.SingleOrDefault(o => o.ID.Equals(id));
            if (order == null) return NotFound();

            if (order.isConfirmed())
            {
                return new OrderStatusResponse { Status = "ready" };
            }
            else
            {
                return new OrderStatusResponse { Status = "processing" };
            }
        }
    }
}
