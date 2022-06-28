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
                confStatus += "\tClient: Refused\n";

            if (IsConfirmedByWarehouse)
                confStatus += "\tWarehouse: Confirmed\n";
            else
                confStatus += "\tWarehouse: Refused\n";

            if (IsConfirmedBySales)
                confStatus += "\tSales: Confirmed\n";
            else
                confStatus += "\tSales: Refused\n";

            if (IsConfirmedByMarketing)
                confStatus += "\tMarketing: Confirmed\n";
            else
                confStatus += "\tMarketing: Refused\n";

            if (IsConfirmedByShipping)
                confStatus += "\tShipping: Confirmed";
            else
                confStatus += "\tShipping: Refused";

            if (IsCanceled)
                confStatus += "\n\t(Order canceled)";

            return confStatus;
        }
    }
}
