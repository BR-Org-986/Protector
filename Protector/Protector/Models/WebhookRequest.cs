using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Protector.Models
{
    public class WebhookRequest
    {
        public string name { get; set; }
        public bool active { get; set; }
        public string[] events { get; set; }
        public Configuration config { get; set; }
    }

    public class Configuration
    {
        public string url { get; set; }
        public string content_type { get; set; }
        public string secret { get; set; }
        public string insecure_ssl { get; set; }
    }
}
