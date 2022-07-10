using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Shipping.Models
{
    public class Method
    {
        [BsonId]
        [BsonElement]
        public string MethodValue { get; set; }

        [BsonElement]
        public double Price { get; set; }

        public Method() { }
        
        public Method(string method, double price)
        {
            this.MethodValue = method;
            this.Price = price;
        }
    }
}
