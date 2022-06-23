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
        double price { get; set; }
    }
    // Message for Marketing with information about new book
    public interface NewBookMarketingInfo
    {
        string ID { get; set; }
        double discount { get; set; }
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
    // Message for Warehouse to check if there is requested amount of books
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
        string BookID { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        int BookQuantity { get; set; }
    }
    public interface ShippingShipmentSent : CorrelatedBy<Guid> { }
    public interface ShippingShipmentNotSent : CorrelatedBy<Guid> { }
    /* ORDER SAGA SECTION END*/
    // Message for Warehouse with request for specified amount of specified book to be send to customer
    public interface WarehouseDeliveryStart : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        int BookQuantity { get; set; }
    }
    // Message for Shipping with confirmation of the delivery
    public interface WarehouseDeliveryStartConfirmation : CorrelatedBy<Guid>
    {
    }
    // Message for Shipping with rejection of the delivery
    public interface WarehouseDeliveryStartRejection : CorrelatedBy<Guid>
    {
    }
}
