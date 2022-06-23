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
    }
}
