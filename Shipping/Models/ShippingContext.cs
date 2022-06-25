using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Shipping.Models
{
    public class ShippingContext : DbContext
    {
        public ShippingContext(DbContextOptions<ShippingContext> options) : base(options)
        { 
        }

        public DbSet<Price> PriceItems { get; set; }
        public DbSet<Method> MethodItems { get; set; }
        public DbSet<Shipment> ShipmentItems { get; set; }
    }
}
