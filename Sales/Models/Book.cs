using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sales.Models
{
    public class Book
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public double Price { get; set; }

        public Book() { }

        public Book(string iD, double price)
        {
            this.ID = iD;
            this.Price = price;
        }
    }
}
