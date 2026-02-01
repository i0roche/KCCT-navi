using System.Collections.Generic;

namespace team1.Models
{
    public class Place
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Building { get; set; } = "";
        public int Floor { get; set; }
        public string Department { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }
}
