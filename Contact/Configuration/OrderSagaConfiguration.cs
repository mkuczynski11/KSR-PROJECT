namespace Contact.Configuration
{
    public class OrderSagaConfiguration
    {
        public int ClientConfirmationTimeoutSeconds { get; set; }
        public int ServicesConfirmationTimeoutSeconds { get; set; }
        public int PaymentTimeoutSeconds { get; set; }
        public int ShipmentTimeoutSeconds { get; set; }
    }
}
