using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Common;
using System;
using Shipping.Models;
using System.Linq;

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

        [HttpGet("price")]
        public IEnumerable<Price> GetPrice()
        {
            return _shippingContext.PriceItems;
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
        //[HttpPut("price")]
        //public ActionResult<Price> PutPrice([FromBody] PriceRequest request)
        //{
        //    //TODO: none of what i tried works
        //    IEnumerable<Price> price = _shippingContext.PriceItems.TakeLast(1);

        //    if (price == null) return NotFound();

        //    price.PriceValue = request.Price;
        //    _shippingContext.SaveChanges();

        //    return price;
        //}
        [HttpPost("methods")]
        public ActionResult<Method> PutMethod([FromBody] MethodRequest request)
        {
            Method method = new Method(request.Method);

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
