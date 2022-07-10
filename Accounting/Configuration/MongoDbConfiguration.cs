namespace Accounting.Configuration
{
    public class MongoDbConfiguration
    {
        public class Collection
        {
            public string Saga { get; set; }
            public string Invoices { get; set; }
        }

        public string Connection { get; set; }
        public string DatabaseName { get; set; }
        public Collection CollectionName { get; set; }
    }
}
