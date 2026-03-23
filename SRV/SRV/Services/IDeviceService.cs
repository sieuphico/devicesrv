using SRV.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SRV.Services
{
    public interface IDeviceService
    {
        Task EnsureDatabaseSetupAsync();

        Task<(List<Model> Models, int TotalCount)> GetWarehouseModelsAsync(
            string category, string modelName, string manufacturer,
            string sortColumn, bool sortAscending,
            int page, int pageSize);

        Task<(List<Device> Devices, int TotalCount)> GetAllDevicesAsync(
            string deviceName, string imei, string modelName,
            string sortColumn, bool sortAscending,
            int page, int pageSize);

        Task BorrowDevicesAsync(int modelId, int quantity);
    }
}
