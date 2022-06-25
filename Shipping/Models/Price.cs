using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Shipping.Models
{
    public class Price
    {
        [Key]
        public string ID { get; set; }
        public double PriceValue { get; set; }

        public Price() { }

        public Price(string ID, double price)
        {
            this.ID = ID;
            this.PriceValue = price;
        }
    }

}
