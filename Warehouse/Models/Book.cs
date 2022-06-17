using System;

namespace Warehouse.Models
{
    public class Book
    {
        public string ID { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }

        public Book() { }

        public Book(string iD, string name, int quantity)
        {
            this.ID = iD;
            this.name = name;
            this.quantity = quantity;
        }
    }
}
