using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contact.Configuration
{
    public class RabbitMQConfiguration
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ReceiveEndpoint { get; set; }
        public int DelayedRedeliverySeconds { get; set; }
    }
}
