using System;

namespace Accounting.Models
{
    public class Invoice
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public bool IsPaid { get; set; }
        public bool IsPublic { get; set; }
        public bool IsCanceled { get; set; }

        public Invoice()
        {
            Text = "";
            IsPaid = false;
            IsPublic = false;
            IsCanceled = false;
        }

        public Invoice(string id)
        {
            ID = id;
            Text = "";
            IsPaid = false;
            IsPublic = false;
            IsCanceled = false;
        }

        public Invoice(string id, string bookID, string bookName, int bookQuantity, 
            double bookPrice, double bookDiscount, string deliveryMethod, double deliveryPrice)
        {
            ID = id;
            IsPaid = false;
            IsPublic = false;
            IsCanceled = false;
            Text = $"{DateTime.Now}\n" +
                $"Order nr: {id}\n" +
                $"Item ID: {bookID}\n" +
                $"Item Name: {bookName}\n" +
                $"Item price: ${bookPrice}\n" +
                $"Quantity: {bookQuantity}\n" +
                $"Discount: {Math.Round(bookDiscount * 100)}%\n" +
                $"Delivery method: {deliveryMethod}\n" +
                $"Delivery price: ${deliveryPrice}\n" +
                $"Total = ${bookPrice * bookQuantity * (1.0 - bookDiscount) + deliveryPrice}";
        }
    }
}
