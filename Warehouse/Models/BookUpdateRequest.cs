﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Warehouse.Models
{
    public class BookUpdateRequest
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
