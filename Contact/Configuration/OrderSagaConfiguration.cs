namespace Contact.Configuration
{
    public class OrderSagaConfiguration
    {
        public int ConfirmationTimeoutSeconds { get; set; }
        public int PaymentTimeoutSeconds { get; set; }
        public int ShipmentTimeoutSeconds { get; set; }
    }
}
