using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marketing
{
    public class Book
    {
        public string ID { get; set; }
        public int discount { get; set; }

        public Book() { }

        public Book(string iD, int discount)
        {
            this.ID = iD;
            this.discount = discount;
        }
    }
}
