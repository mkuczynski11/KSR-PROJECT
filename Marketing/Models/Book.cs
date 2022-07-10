using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketing.Models
{
    public class Book
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public double Discount { get; set; }

        public Book() { }

        public Book(string iD, double discount)
        {
            this.ID = iD;
            this.Discount = discount;
        }
    }
}
