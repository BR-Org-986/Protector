using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Protector.Models
{
    public class WebhookResponse
    {
        public int id { get; set; }
        public string url { get; set; }
        public string ping_url { get; set; }
        public string name { get; set; }
        public string[] events { get; set; }
        public bool active { get; set; }
        public Configuration config { get; set; }
        public string updated_at { get; set; }
        public string created_at { get; set; }
    }
}
