namespace Contact.Configuration
{
    public class MongoDbConfiguration
    {
        public class Collection
        {
            public string Saga { get; set; }
            public string Orders { get; set; }
        }

        public string Connection { get; set; }
        public string DatabaseName { get; set; }
        public Collection CollectionName { get; set; }
    }
}
