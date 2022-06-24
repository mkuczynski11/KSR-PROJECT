using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Shipping.Models
{
    public class Shipment
    {
        [Key]
        public string ID { get; set; }
        public bool IsConfirmedByWarehouse { get; set; }
        public string BookID { get; set; }
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
