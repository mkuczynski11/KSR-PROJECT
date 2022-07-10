using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using Shipping.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shipping.Configuration;
using Microsoft.Extensions.Configuration;

namespace Shipping.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ILogger<ShippingController> _logger;
        private readonly MongoClient _mongoClient;
        private readonly MongoDbConfiguration _mongoConf;
        public readonly IPublishEndpoint _publishEndpoint;

        public ShippingController(IPublishEndpoint publishEndpoint, MongoClient mongoClient, ILogger<ShippingController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _mongoClient = mongoClient;
            _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            _publishEndpoint = publishEndpoint;
        }
        [HttpGet("methods")]
        public IEnumerable<Method> GetMethods()
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Method>(_mongoConf.CollectionName.Methods);
            return collection.Find(_ => true).ToList();
        }
        [HttpGet("shipments")]
        public IEnumerable<Shipment> GetShipments()
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Shipment>(_mongoConf.CollectionName.Shipments);
            return collection.Find(_ => true).ToList();
        }
        [HttpPost("methods")]
        public ActionResult<Method> PostMethod([FromBody] MethodRequest request)
        {
            Method method = new Method(request.Method, request.Price);

            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Method>(_mongoConf.CollectionName.Methods);

            collection.InsertOne(method);

            return method;
        }
        [HttpDelete("methods")]
        public ActionResult<Method> DeleteMethod([FromBody] MethodRequest request)
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Method>(_mongoConf.CollectionName.Methods);

            Method method = collection.Find(m => m.MethodValue.Equals(request.Method)).SingleOrDefault();

            if (method == null) return NotFound();

            var result = collection.DeleteOne(m => m.MethodValue.Equals(request.Method));

            return method;
        }
        [HttpPut("methods")]
        public ActionResult<Method> PutMethod([FromBody] MethodRequest request)
        {
            var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Method>(_mongoConf.CollectionName.Methods);

            Method method = collection.Find(m => m.MethodValue.Equals(request.Method)).SingleOrDefault();

            if (method == null) return NotFound();

            method.MethodValue = request.Method;
            method.Price = request.Price;
            collection.ReplaceOne(m => m.MethodValue.Equals(request.Method), method);

            return method;
        }
    }
}
