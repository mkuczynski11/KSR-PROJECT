using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Models
{
    public class Order
    {
        [BsonId]
        [BsonElement]
        public string ID { get; set; }

        [BsonElement]
        public bool IsConfirmedByClient { get; set; }

        [BsonElement]
        public bool IsConfirmedByWarehouse { get; set; }

        [BsonElement]
        public bool IsConfirmedBySales { get; set; }

        [BsonElement]
        public bool IsConfirmedByMarketing { get; set; }

        [BsonElement]
        public bool IsConfirmedByShipping { get; set; }

        [BsonElement]
        public bool IsPaid { get; set; }

        [BsonElement]
        public bool IsShipped { get; set; }

        [BsonElement]
        public bool IsCanceled { get; set; }

        public Order() 
        {
            IsConfirmedByClient = false;
            IsConfirmedByWarehouse = false;
            IsConfirmedBySales = false;
            IsConfirmedByMarketing = false;
            IsConfirmedByShipping = false;
            IsPaid = false;
            IsShipped = false;
            IsCanceled = false;
        }

        public Order(string iD)
        {
            ID = iD;
            IsConfirmedByClient = false;
            IsConfirmedByWarehouse = false;
            IsConfirmedBySales = false;
            IsConfirmedByMarketing = false;
            IsConfirmedByShipping = false;
            IsPaid = false;
            IsShipped = false;
            IsCanceled = false;
        }

        public bool isConfirmed()
        {
            return IsConfirmedByClient && IsConfirmedByWarehouse && IsConfirmedBySales &&
                IsConfirmedByMarketing && IsConfirmedByShipping;
        }

        public override string ToString()
        {
            string confStatus = "";

            if (IsConfirmedByClient)
                confStatus += "\tClient: Confirmed\n";
            else
                confStatus += "\tClient: Not confirmed\n";

            if (IsConfirmedByWarehouse)
                confStatus += "\tWarehouse: Confirmed\n";
            else
                confStatus += "\tWarehouse: Not confirmed\n";

            if (IsConfirmedBySales)
                confStatus += "\tSales: Confirmed\n";
            else
                confStatus += "\tSales: Not confirmed\n";

            if (IsConfirmedByMarketing)
                confStatus += "\tMarketing: Confirmed\n";
            else
                confStatus += "\tMarketing: Not confirmed\n";

            if (IsConfirmedByShipping)
                confStatus += "\tShipping: Confirmed";
            else
                confStatus += "\tShipping: Not confirmed";

            if (IsCanceled)
                confStatus += "\n\t(Order canceled)";

            return confStatus;
        }
    }
}
