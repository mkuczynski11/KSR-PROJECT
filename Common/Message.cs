using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public interface NewBookSalesInfo
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
    public interface NewBookMarketingInfo
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
    public interface AccountingInvoicePaymentTimeoutExpired
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
    public interface ContactOrderClientConfirmationTimeoutExpired
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderClientConfirmationTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactOrderServicesConfirmationTimeoutExpired
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderServicesConfirmationTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactOrderPaymentTimeoutExpired
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactOrderPaymentTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactShipmentTimeoutExpired
    {
        Guid OrderId { get; }
        string Print()
        {
            return $"ContactShipmentTimeoutExpired{{" +
                $"OrderId={OrderId}}}";
        }
    }
    public interface ContactConfirmationConfirmedByAllParties : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ContactConfirmationConfirmedByAllParties{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ContactConfirmationRefusedByAtLeastOneParty : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ContactConfirmationRefusedByAtLeastOneParty{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface OrderStart : CorrelatedBy<Guid>
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
    public interface OrderCancel : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"OrderCancel{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ClientConfirmationAccept : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ClientConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ClientConfirmationRefuse : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ClientConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseConfirmation : CorrelatedBy<Guid>
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
    public interface WarehouseConfirmationAccept : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"WarehouseConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseConfirmationRefuse : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"WarehouseConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface SalesConfirmation : CorrelatedBy<Guid>
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
    public interface SalesConfirmationAccept : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"SalesConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface SalesConfirmationRefuse : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"SalesConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface MarketingConfirmation : CorrelatedBy<Guid>
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
    public interface MarketingConfirmationAccept : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"MarketingConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface MarketingConfirmationRefuse : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"MarketingConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingConfirmation : CorrelatedBy<Guid>
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
    public interface ShippingConfirmationAccept : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ShippingConfirmationAccept{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingConfirmationRefuse : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ShippingConfirmationRefuse{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceStart : CorrelatedBy<Guid>
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
    public interface AccountingInvoicePublish : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"AccountingInvoicePublish{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceCancel : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"AccountingInvoiceCancel{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoicePaid : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"AccountingInvoicePaid{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface AccountingInvoiceNotPaid : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"AccountingInvoiceNotPaid{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingShipmentStart : CorrelatedBy<Guid>
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
    public interface ShippingShipmentSent : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ShippingShipmentSent{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface ShippingShipmentNotSent : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"ShippingShipmentNotSent{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    /* ORDER SAGA SECTION END*/

    public interface ShippingWarehouseDeliveryConfirmationTimeoutExpired
    {
        Guid ShippingId { get; }
        string Print()
        {
            return $"ShippingWarehouseDeliveryConfirmationTimeoutExpired{{" +
                $"ShippingId={ShippingId}}}";
        }
    }
    public interface WarehouseDeliveryStart : CorrelatedBy<Guid>
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
    public interface WarehouseDeliveryStartConfirmation : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"WarehouseDeliveryStartConfirmation{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
    public interface WarehouseDeliveryStartRejection : CorrelatedBy<Guid>
    {
        string Print()
        {
            return $"WarehouseDeliveryStartRejection{{" +
                $"CorrelationId={CorrelationId}}}";
        }
    }
}
