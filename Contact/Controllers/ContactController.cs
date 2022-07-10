using Common;
using Contact.Models;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Contact.Configuration;

namespace Contact.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger;
        private readonly MongoClient _mongoClient;
        private readonly MongoDbConfiguration _mongoConf;
        public readonly IPublishEndpoint _publishEndpoint;

        public ContactController(IPublishEndpoint publishEndpoint, MongoClient mongoClient, ILogger<ContactController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _mongoClient = mongoClient;
            _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("orders/create")]
        public ActionResult<OrderCreateResponse> StartOrder([FromBody] OrderCreateRequest request)
        {
            _logger.LogInformation($"New Order: data={request}");
            Guid ID = Guid.NewGuid();

            Order order = new Order(ID.ToString());

            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Order>(_mongoConf.CollectionName.Orders);
            collection.InsertOne(order);

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
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Order>(_mongoConf.CollectionName.Orders);
            Order order = collection.Find(o => o.ID.Equals(id)).SingleOrDefault();

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
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Order>(_mongoConf.CollectionName.Orders);
            Order order = collection.Find(o => o.ID.Equals(id)).SingleOrDefault();

            if (order == null) return NotFound();

            _publishEndpoint.Publish<ClientConfirmationRefuse>(new
            {
                CorrelationId = order.ID
            });

            return Ok();
        }

        [HttpGet("orders/{id}/status")]
        public ActionResult<OrderStatusResponse> GetStatus(string id)
        {
            _logger.LogInformation($"Order status with ID:{id} requested");

            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Order>(_mongoConf.CollectionName.Orders);
            Order order = collection.Find(o => o.ID.Equals(id)).SingleOrDefault();

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
