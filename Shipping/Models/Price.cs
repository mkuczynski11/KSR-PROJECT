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
        public double price { get; set; }

        public Price() { }

        public Price(double price)
        {
            this.price = price;
        }
    }

}
