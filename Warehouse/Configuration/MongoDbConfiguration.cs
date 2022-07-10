namespace Warehouse.Configuration
{
    public class MongoDbConfiguration
    {
        public class Collection
        {
            public string Books { get; set; }
            public string Reservations { get; set; }
        }

        public string Connection { get; set; }
        public string DatabaseName { get; set; }
        public Collection CollectionName { get; set; }
    }
}
