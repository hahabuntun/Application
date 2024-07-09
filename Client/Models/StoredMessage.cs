using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class StoredMessage
    {
        public string From { get; set; }
        public DateTime Time { get; set; }
        public string Text { get; set; }
        public string ImagePath { get; set; }
        public string Color { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
    }
}
