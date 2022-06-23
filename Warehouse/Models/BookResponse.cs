using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.Models
{
    public class BookResponse
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public double price { get; set; }
        public double discount { get; set; }
    }
}
