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
}
