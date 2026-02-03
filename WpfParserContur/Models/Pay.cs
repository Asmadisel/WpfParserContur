using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfParserContur.Models
{
    public class Pay
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public decimal Total { get; set; }
    }
}
