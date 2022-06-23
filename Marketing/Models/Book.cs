using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketing.Models
{
    public class Book
    {
        public string ID { get; set; }
        public double discount { get; set; }

        public Book() { }

        public Book(string iD, double discount)
        {
            this.ID = iD;
            this.discount = discount;
        }
    }
}
