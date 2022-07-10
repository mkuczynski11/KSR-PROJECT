using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace Shipping.Models
{
    public class Shipment
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public bool IsConfirmedByWarehouse { get; set; }

        [BsonElement]
        public string BookID { get; set; }

        [BsonElement]
        public int Quantity { get; set; }

        public Shipment() { }

        public Shipment(string ID, bool IsConfirmedByWarehouse, string BookID, int Quantity)
        {
            this.ID = ID;
            this.IsConfirmedByWarehouse = IsConfirmedByWarehouse;
            this.BookID = BookID;
            this.Quantity = Quantity;
        }
    }
}
