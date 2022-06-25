using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketing.Models
{
    public class BookUpdateRequest
    {
        public string BookID { get; set; }
        public double BookDiscount { get; set; }
    }
}
