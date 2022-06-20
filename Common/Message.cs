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
        int price { get; set; }
    }
    public interface NewBookMarketingInfo
    {
        string ID { get; set; }
        int discount { get; set; }
    }

    public interface NewOrderStart : CorrelatedBy<Guid>
    {
        string BookID { get; set; }
        string BookName { get; set; }
        string BookQuantity { get; set; }
        double BookPrice { get; set; }
        double BookDiscount { get; set; }
        string DeliveryMethod { get; set; }
        string DeliveryPrice { get; set; }
    }
}
