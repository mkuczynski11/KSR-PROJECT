using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Warehouse.Models
{
    public class Book
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public string Name { get; set; }

        [BsonElement]
        public int Quantity { get; set; }

        public Book() { }

        public Book(string iD, string name, int quantity)
        {
            this.ID = iD;
            this.Name = name;
            this.Quantity = quantity;
        }
    }
}
