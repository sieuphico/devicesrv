namespace SRV.Data
{
    public class Device
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public string? Name { get; set; }
        public string? Imei { get; set; }
        public string? SerialLab { get; set; }
        public string? SerialNumber { get; set; }
        public string? Cicuiseri { get; set; }
        public string? HwVersion { get; set; }
        
        // Thêm trường kiểm soát thiết bị đã được lấy ra khỏi kho
        public bool IsBorrowed { get; set; } = false;

        public Model? Model { get; set; }
    }
}
