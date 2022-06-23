using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sales.Models
{
    public class Book
    {
        public string ID { get; set; }
        public double price { get; set; }

        public Book() { }

        public Book(string iD, double price)
        {
            this.ID = iD;
            this.price = price;
        }
    }
}
