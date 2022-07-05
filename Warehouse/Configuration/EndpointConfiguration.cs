namespace Warehouse.Configuration
{
    public class EndpointConfiguration
    {
        public string DeliveryRequestConsumer { get; set; }
        public string ConfirmationConsumer { get; set; }
        public string OrderCancelConsumer { get; set; }
    }
}
