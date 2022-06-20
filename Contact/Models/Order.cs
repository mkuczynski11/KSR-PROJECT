using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Models
{
    public class Order
    {
        public string ID { get; set; }
        public bool IsConfirmedByClient { get; set; }
        public bool IsConfirmedByWarehouse { get; set; }
        public bool IsConfirmedBySales { get; set; }
        public bool IsConfirmedByMarketing { get; set; }
        public bool IsConfirmedByShipping { get; set; }
        public bool IsPaid { get; set; }
        public bool IsShipped { get; set; }

        public Order() 
        {
            IsConfirmedByClient = false;
            IsConfirmedByWarehouse = false;
            IsConfirmedBySales = false;
            IsConfirmedByMarketing = false;
            IsConfirmedByShipping = false;
            IsPaid = false;
            IsShipped = false;
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
        }

        public bool isConfirmed()
        {
            return IsConfirmedByClient && IsConfirmedByWarehouse && IsConfirmedBySales &&
                IsConfirmedByMarketing && IsConfirmedByShipping;
        }
    }
}
