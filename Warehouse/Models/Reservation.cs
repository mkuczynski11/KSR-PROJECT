using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.Models
{
    public class Reservation
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public string BookID { get; set; }

        [BsonElement]
        public int Quantity { get; set; }

        [BsonElement]
        public bool IsRedeemed { get; set; }

        [BsonElement]
        public bool IsCancelled { get; set; }

        public Reservation() { }
        
        public Reservation(string ID, string BookID, int Quantity, bool IsRedeemed, bool IsCancelled)
        {
            this.ID = ID;
            this.BookID = BookID;
            this.Quantity = Quantity;
            this.IsRedeemed = IsRedeemed;
            this.IsCancelled = IsCancelled;
        }
    }
}
