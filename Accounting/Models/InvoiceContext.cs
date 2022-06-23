using Microsoft.EntityFrameworkCore;

namespace Accounting.Models
{
    public class InvoiceContext : DbContext
    {
        public InvoiceContext(DbContextOptions<InvoiceContext> options) : base(options)
        {
        }

        public DbSet<Invoice> InvoiceItems { get; set; }
    }
}
