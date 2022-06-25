namespace Accounting.Models
{
    public class InvoiceResponse
    {
        public string Text { get; set; }
        public bool IsPaid { get; set; }
        public bool IsCanceled { get; set; }
    }
}
