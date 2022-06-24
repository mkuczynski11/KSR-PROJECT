using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Shipping.Models
{
    public class Method
    {
        [Key]
        public string MethodValue { get; set; }

        public Method() { }
        
        public Method(string method)
        {
            this.MethodValue = method;
        }
    }
}
