using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Common;
using System;
using Shipping.Models;

namespace Shipping.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ShippingContext _shippingContext;
        public readonly IPublishEndpoint _publishEndpoint;

        public ShippingController(IPublishEndpoint publishEndpoint, ShippingContext shippingContext)
        {
            _shippingContext = shippingContext;
            _publishEndpoint = publishEndpoint;
        }
        [HttpGet("methods")]
        public IEnumerable<Method> GetMethods()
        {
            return _shippingContext.MethodItems;
        }
        [HttpGet("shipments")]
        public IEnumerable<Shipment> GetShipments()
        {
            return _shippingContext.ShipmentItems;
        }
        [HttpPost("methods")]
        public ActionResult<Method> PutMethod([FromBody] MethodRequest request)
        {
            Method method = new Method(request.Method, request.Price);

            _shippingContext.MethodItems.Add(method);
            _shippingContext.SaveChanges();

            return method;
        }
        [HttpDelete("methods")]
        public ActionResult<Method> DeleteMethod([FromBody] MethodRequest request)
        {
            Method method = _shippingContext.MethodItems.SingleOrDefault(m => m.MethodValue.Equals(request.Method));

            if (method == null) return NotFound();

            _shippingContext.MethodItems.Remove(method);
            _shippingContext.SaveChanges();

            return method;
        }
    }
}
