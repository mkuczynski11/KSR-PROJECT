using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sales.Models
{
    public class Book
    {
        public string ID { get; set; }
        public int price { get; set; }

        public Book() { }

        public Book(string iD, int price)
        {
            this.ID = iD;
            this.price = price;
        }
    }
}
