using System;

namespace Warehouse.Models
{
    public class Book
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }

        public Book() { }

        public Book(string iD, string name, int quantity)
        {
            this.ID = iD;
            this.Name = name;
            this.Quantity = quantity;
        }
    }
}
