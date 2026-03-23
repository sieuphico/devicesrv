using Microsoft.EntityFrameworkCore;
using Npgsql;
using SRV.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SRV.Services
{
    public class DeviceService : IDeviceService
    {
        public async Task EnsureDatabaseSetupAsync()
        {
            using var db = new DeviceDbContext();
            db.Database.EnsureCreated();
            
            // Đảm bảo có cột IsBorrowed
            await db.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE ""Devices"" ADD COLUMN IF NOT EXISTS ""IsBorrowed"" boolean NOT NULL DEFAULT FALSE;
                CREATE INDEX IF NOT EXISTS ix_devices_isborrowed ON ""Devices"" (""IsBorrowed"");
            ");
        }

        public async Task<(List<Model> Models, int TotalCount)> GetWarehouseModelsAsync(
            string category, string modelName, string manufacturer,
            string sortColumn, bool sortAscending,
            int page, int pageSize)
        {
            using var db = new DeviceDbContext();
            var query = db.Models.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(m => m.Category != null && EF.Functions.ILike(m.Category, $"%{category}%"));
            if (!string.IsNullOrEmpty(modelName))
                query = query.Where(m => m.Name != null && EF.Functions.ILike(m.Name, $"%{modelName}%"));
            if (!string.IsNullOrEmpty(manufacturer))
                query = query.Where(m => m.Manufacturer != null && EF.Functions.ILike(m.Manufacturer, $"%{manufacturer}%"));

            int totalCount = await query.CountAsync();

            query = sortColumn switch
            {
                "Name" => sortAscending ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                "Category" => sortAscending ? query.OrderBy(m => m.Category) : query.OrderByDescending(m => m.Category),
                "Manufacturer" => sortAscending ? query.OrderBy(m => m.Manufacturer) : query.OrderByDescending(m => m.Manufacturer),
                "Available" => sortAscending ? query.OrderBy(m => m.Available) : query.OrderByDescending(m => m.Available),
                _ => query.OrderBy(m => m.Id)
            };

            var models = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (models, totalCount);
        }

        public async Task<(List<Device> Devices, int TotalCount)> GetAllDevicesAsync(
            string deviceName, string imei, string modelName,
            string sortColumn, bool sortAscending,
            int page, int pageSize)
        {
            using var db = new DeviceDbContext();
            var query = db.Devices.Include(d => d.Model).AsQueryable();

            if (!string.IsNullOrEmpty(deviceName))
                query = query.Where(d => d.Name != null && EF.Functions.ILike(d.Name, $"%{deviceName}%"));
            if (!string.IsNullOrEmpty(imei))
                query = query.Where(d => d.Imei != null && EF.Functions.ILike(d.Imei, $"%{imei}%"));
            if (!string.IsNullOrEmpty(modelName))
                query = query.Where(d => d.Model != null && d.Model.Name != null && EF.Functions.ILike(d.Model.Name, $"%{modelName}%"));

            int totalCount = await query.CountAsync();

            query = sortColumn switch
            {
                "Name" => sortAscending ? query.OrderBy(d => d.Name) : query.OrderByDescending(d => d.Name),
                "Imei" => sortAscending ? query.OrderBy(d => d.Imei) : query.OrderByDescending(d => d.Imei),
                "ModelName" => sortAscending ? query.OrderBy(d => d.Model!.Name) : query.OrderByDescending(d => d.Model!.Name),
                "IsBorrowed" => sortAscending ? query.OrderBy(d => d.IsBorrowed) : query.OrderByDescending(d => d.IsBorrowed),
                _ => query.OrderBy(d => d.Id)
            };

            var devices = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (devices, totalCount);
        }

        public async Task BorrowDevicesAsync(int modelId, int quantity)
        {
            using var db = new DeviceDbContext();

            // Tối ưu để mượn lên tới hàng triệu records không load Data vào RAM
            // Dùng Common Table Expression (CTE) làm một SQL Transaction nhanh chóng mặt 
            var sql = @"
            WITH cte AS (
                SELECT ""Id"" 
                FROM ""Devices"" 
                WHERE ""ModelId"" = @modelId AND ""IsBorrowed"" = false 
                LIMIT @quantity
            )
            UPDATE ""Devices"" 
            SET ""IsBorrowed"" = true 
            FROM cte 
            WHERE ""Devices"".""Id"" = cte.""Id"";
            ";
            await db.Database.ExecuteSqlRawAsync(sql, 
                new NpgsqlParameter("@modelId", modelId), 
                new NpgsqlParameter("@quantity", quantity));

            // Khớp đồng bộ Available với thực tế
            var updateModelSql = @"
            UPDATE ""Models""
            SET ""Available"" = (SELECT count(*) FROM ""Devices"" WHERE ""ModelId"" = @modelId AND ""IsBorrowed"" = false)
            WHERE ""Id"" = @modelId;
            ";
            await db.Database.ExecuteSqlRawAsync(updateModelSql, new NpgsqlParameter("@modelId", modelId));

            // Nháy chuông Listeners Multi-instances qua Postgres
            await db.Database.ExecuteSqlRawAsync(@"NOTIFY db_change, 'borrow';");
        }
    }
}
