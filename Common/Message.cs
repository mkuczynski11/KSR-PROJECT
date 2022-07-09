using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public interface BaseMessage { }
    public interface NewBookSalesInfo : BaseMessage
    {
        string ID { get; set; }
        double price { get; set; }
        string Print()
        {
            return $"NewBookSalesInfo{{" +
                $"ID={ID}," +
                $"price={price}}}";
        }
    }
    public interface NewBookMarketingInfo : BaseMessage
    {
        string ID { get; set; }
        double discount { get; set; }
        string Print()
        {
            return $"NewBookMarketingInfo{{" +
                $"ID={ID}," +
                $"discount={discount}}}";
        }
    }

    /* INVOICE SAGA SECTION */
    public interface AccountingInvoicePaymentTimeoutExpired : BaseMessage
    {
        Guid InvoiceId { get; }
        string Print()
        {
            return $"AccountingInvoicePaymentTimeoutExpired{{" +
                $"InvoiceId={InvoiceId}}}";
        }
    }
    /* INVOICE SAGA SECTION END */

    /* ORDER SAGA SECTION */
    public interface ContactOrderClientConfirmationTimeoutExpired : BaseMessage
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderClientConfirmationTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactOrderServicesConfirmationTimeoutExpired : BaseMessage
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderServicesConfirmationTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactOrderPaymentTimeoutExpired : BaseMessage
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderPaymentTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactShipmentTimeoutExpired : BaseMessage
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactShipmentTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactConfirmationConfirmedByAllParties : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ContactConfirmationConfirmedByAllParties{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ContactConfirmationRefusedByAtLeastOneParty : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ContactConfirmationRefusedByAtLeastOneParty{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface OrderStart : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
        double BookPrice { get; set; }
        double BookDiscount { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        string Print()
        {
            return $"OrderStart{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookName={BookName}," +
                $"BookQuantity={BookQuantity}," +
                $"BookPrice={BookPrice}," +
                $"BookDiscount={BookDiscount}," +
                $"DeliveryMethod={DeliveryMethod}," +
                $"DeliveryPrice={DeliveryPrice}}}";
        }
    }
    public interface OrderCancel : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"OrderCancel{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ClientConfirmationAccept : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ClientConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ClientConfirmationRefuse : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ClientConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseConfirmation : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
        string Print()
        {
            return $"WarehouseConfirmation{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookName={BookName}," +
                $"BookQuantity={BookQuantity}}}";
        }
    }
    public interface WarehouseConfirmationAccept : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"WarehouseConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseConfirmationRefuse : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"WarehouseConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface SalesConfirmation : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        double BookPrice { get; set; }
        string Print()
        {
            return $"SalesConfirmation{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookPrice={BookPrice}}}";
        }
    }
    public interface SalesConfirmationAccept : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"SalesConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface SalesConfirmationRefuse : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"SalesConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface MarketingConfirmation : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        double BookDiscount { get; set; }
        string Print()
        {
            return $"MarketingConfirmation{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookDiscount={BookDiscount}}}";
        }
    }
    public interface MarketingConfirmationAccept : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"MarketingConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface MarketingConfirmationRefuse : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"MarketingConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingConfirmation : CorrelatedBy<Guid>, BaseMessage
    {
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        string Print()
        {
            return $"ShippingConfirmation{{" +
                $"CorrelationId={CorrelationId}," +
                $"DeliveryMethod={DeliveryMethod}," +
                $"DeliveryPrice={DeliveryPrice}}}";
        }
    }
    public interface ShippingConfirmationAccept : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ShippingConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingConfirmationRefuse : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ShippingConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceStart : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        string BookName { get; set; }
        int BookQuantity { get; set; }
        double BookPrice { get; set; }
        double BookDiscount { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        string Print()
        {
            return $"OrderStart{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookName={BookName}," +
                $"BookQuantity={BookQuantity}," +
                $"BookPrice={BookPrice}," +
                $"BookDiscount={BookDiscount}," +
                $"DeliveryMethod={DeliveryMethod}," +
                $"DeliveryPrice={DeliveryPrice}}}";
        }
    }
    public interface AccountingInvoicePublish : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"AccountingInvoicePublish{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceCancel : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"AccountingInvoiceCancel{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoicePaid : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"AccountingInvoicePaid{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceNotPaid : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"AccountingInvoiceNotPaid{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingShipmentStart : CorrelatedBy<Guid>, BaseMessage
    {
        string BookID { get; set; }
        string DeliveryMethod { get; set; }
        double DeliveryPrice { get; set; }
        int BookQuantity { get; set; }
        string Print()
        {
            return $"ShippingShipmentStart{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookQuantity={BookQuantity}," +
                $"DeliveryMethod={DeliveryMethod}," +
                $"DeliveryPrice={DeliveryPrice}}}";
        }
    }
    public interface ShippingShipmentSent : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ShippingShipmentSent{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingShipmentNotSent : CorrelatedBy<Guid>, BaseMessage
    {
        string Print()
        {
            return $"ShippingShipmentNotSent{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    /* ORDER SAGA SECTION END*/

    public interface ShippingWarehouseDeliveryConfirmationTimeoutExpired : BaseMessage
    {
        Guid ShippingId { get; }
        string Print()
        {
            return $"ShippingWarehouseDeliveryConfirmationTimeoutExpired{{" +
                $"ShippingId={ShippingId}}}";
        }
    }
    public interface WarehouseDeliveryStart : CorrelatedBy<Guid>, BaseMessage
{
        string BookID { get; set; }
        int BookQuantity { get; set; }
        string Print()
        {
            return $"WarehouseDeliveryStart{{" +
                $"CorrelationId={CorrelationId}," +
                $"BookID={BookID}," +
                $"BookQuantity={BookQuantity}}}";
        }
    }
    public interface WarehouseDeliveryStartConfirmation : CorrelatedBy<Guid>, BaseMessage
{
        string Print()
        {
            return $"WarehouseDeliveryStartConfirmation{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseDeliveryStartRejection : CorrelatedBy<Guid>, BaseMessage
{
        string Print()
        {
            return $"WarehouseDeliveryStartRejection{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
}
