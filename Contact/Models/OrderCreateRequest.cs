namespace Contact.Models
{
    public class OrderCreateRequest
    {
        public string BookID { get; set; }
        public string BookName { get; set; }
        public int BookQuantity { get; set; }
        public double BookPrice { get; set; }
        public double BookDiscount { get; set; }
        public string DeliveryMethod { get; set; }
        public double DeliveryPrice { get; set; }

        public override string ToString()
        {
            return $"{{BookID={BookID}, BookName={BookName}, BookQuantity={BookQuantity}, " +
                $"BookPrice={BookPrice}, BookDiscount={BookDiscount}, DeliveryMethod={DeliveryMethod}" +
                $"DeliveryPrice={DeliveryPrice}}}";
        }
    }
}
