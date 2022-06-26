using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.Models
{
    public class Reservation
    {
        public string ID { get; set; }
        public string BookID { get; set; }
        public int Quantity { get; set; }
        public bool IsRedeemed { get; set; }
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
