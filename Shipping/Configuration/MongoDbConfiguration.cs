﻿namespace Shipping.Configuration
{
    public class MongoDbConfiguration
    {
        public class Collection
        {
            public string Saga { get; set; }
            public string Methods { get; set; }
            public string Shipments { get; set; }
        }

        public string Connection { get; set; }
        public string DatabaseName { get; set; }
        public Collection CollectionName { get; set; }
    }
}
