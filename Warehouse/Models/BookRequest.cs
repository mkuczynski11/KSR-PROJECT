﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.Models
{
    public class BookRequest
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double Discount { get; set; }
    }
}
