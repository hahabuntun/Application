using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Models
{
    public class StoredMessage
    {
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ClientAddress { get; set; }
        public int ClientPort { get; set; }
        public int? Id { get; set; }
        public string Text { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string FormatVersion { get; set; }
        public string Color { get; set; }
        public string ImagePath { get; set; }
    }
}
