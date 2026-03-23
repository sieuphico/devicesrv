using System.Collections.Generic;

namespace SRV.Data
{
    public class Model
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Manufacturer { get; set; }
        public string? Category { get; set; }
        public string? Subcategory { get; set; }
        public int Available { get; set; }

        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
