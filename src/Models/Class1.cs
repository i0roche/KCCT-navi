using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace team1.Models
{
    internal class Place
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Building { get; set; } = "";
        public int Floor { get; set; }
        public string Department { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }
}
