using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse
{
    public class BookRequest
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int price { get; set; }
        public int discount { get; set; }
    }
}
