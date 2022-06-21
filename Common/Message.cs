using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;

namespace Common
{
    // Message for Sales with information about new book
    public interface NewBookSalesInfo
    {
        string ID { get; set; }
        int price { get; set; }
    }
    // Message for Marketing with information about new book
    public interface NewBookMarketingInfo
    {
        string ID { get; set; }
        int discount { get; set; }
    }

    /* ORDER SAGA SECTION */
    public interface OrderStart : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
        double BookPrice { get; set; }
        double BookDiscount { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
    }
    public interface OrderCancel : CorrelatedBy<Guid> { }
    public interface ClientConfirmationAccept : CorrelatedBy<Guid> { }
    public interface ClientConfirmationRefuse : CorrelatedBy<Guid> { }
    public interface WarehouseConfirmation : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
    }
    public interface WarehouseConfirmationAccept : CorrelatedBy<Guid> { }
    public interface WarehouseConfirmationRefuse : CorrelatedBy<Guid> { }
    public interface SalesConfirmation : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        double BookPrice { get; set; }
    }
    public interface SalesConfirmationAccept : CorrelatedBy<Guid> { }
    public interface SalesConfirmationRefuse : CorrelatedBy<Guid> { }
    public interface MarketingConfirmation : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        double BookDiscount { get; set; }
    }
    public interface MarketingConfirmationAccept : CorrelatedBy<Guid> { }
    public interface MarketingConfirmationRefuse : CorrelatedBy<Guid> { }
    public interface ShippingConfirmation : CorrelatedBy<Guid>
    {
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
    }
    public interface ShippingConfirmationAccept : CorrelatedBy<Guid> { }
    public interface ShippingConfirmationRefuse : CorrelatedBy<Guid> { }
    public interface AccountingInvoiceStart : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
        double BookPrice { get; set; }
        double BookDiscount { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
    }
    public interface AccountingInvoicePublish : CorrelatedBy<Guid> { }
    public interface AccountingInvoiceCancel : CorrelatedBy<Guid> { }
    public interface AccountingInvoicePaid : CorrelatedBy<Guid> { }
    public interface AccountingInvoiceNotPaid : CorrelatedBy<Guid> { }
    public interface ShippingShipmentStart : CorrelatedBy<Guid>
    {
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        int BookQuantity { get; set; }
    }
    public interface ShippingShipmentSent : CorrelatedBy<Guid> { }
    public interface ShippingShipmentNotSent : CorrelatedBy<Guid> { }
    /* ORDER SAGA SECTION END*/
    // Message for Warehouse to check if there is requested amount of books
    public interface BookQuantityCheck
    {
        string ID { get; set; }
        int quantity { get; set; }
    }
    // Message for Contact to confirm quantity of book
    public interface BookQuantityConfirmation
    {
    }
    // Message for Contact to reject quantity of book
    public interface BookQuantityRejection
    {
    }
    // Message for Shipping to validate delivery price and method existance
    public interface DeliveryCheck
    {
        double price { get; set; }
        string method { get; set; }
    }
    // Message for Contact to confirm delivery information
    public interface DeliveryInfoConfirmation
    {
    }
    // Message for Contact to reject delivery information
    public interface DeliveryInfoRejection
    {
    }
    // Message for Shipping with request for shipping for specified book
    public interface ShippingRequest : CorrelatedBy<Guid>
    {
        string ID { get; set; }
        int quantity { get; set; }
    }
    // Message for Warehouse with request for specified amount of specified book to be send to customer
    public interface WarehouseDeliveryRequest : CorrelatedBy<Guid>
    {
        string ID { get; set; }
        int quantity { get; set; }
    }
    // Message for Shipping with confirmation of the delivery
    public interface WarehouseDeliveryConfirmation : CorrelatedBy<Guid>
    {
    }
    // Message for Shipping with rejection of the delivery
    public interface WarehouseDeliveryRejection : CorrelatedBy<Guid>
    {
    }
    // Message for Contact with confirmation of the shipping
    public interface ShippingConfirmed
    {
    }
    // Message for Contact with rejection of the shipping
    public interface ShippingRejected
    {
    }
}
