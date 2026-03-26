using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DeviceSrv.Models;
using Npgsql;

namespace DeviceSrv.Services
{
    public class DeviceService
    {
        private readonly string _connectionString = "Host=localhost;Port=5432;Database=DeviceSrvDb;Username=postgres;Password=root";

        public async Task<IEnumerable<Model>> GetModelsAsync(string orderBy = "Name", bool isDescending = false)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var validColumns = new[] { "Id", "Name", "Manufacturer", "Category", "Subcategory", "Available" };
            if (!validColumns.Contains(orderBy)) orderBy = "Name";
            var sortDir = isDescending ? "DESC" : "ASC";
            
            return await connection.QueryAsync<Model>($"SELECT * FROM public.\"Models\" ORDER BY \"{orderBy}\" {sortDir}");
        }

        public async Task<(IEnumerable<Device> Devices, int TotalCount)> GetDevicesPagedAsync(int pageNumber, int pageSize, string orderBy = "Id", bool isDescending = false, string filter = "")
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;
            
            var validColumns = new[] { "Id", "Name", "Imei", "SerialNumber", "IsBorrowed" };
            if (!validColumns.Contains(orderBy)) orderBy = "Id";
            var sortDir = isDescending ? "DESC" : "ASC";
            
            var sql = $"SELECT * FROM public.\"Devices\" WHERE \"Name\" ILIKE @Filter OR \"Imei\" ILIKE @Filter OR \"SerialNumber\" ILIKE @Filter ORDER BY \"{orderBy}\" {sortDir} LIMIT @PageSize OFFSET @Offset";
            var countSql = "SELECT COUNT(*) FROM public.\"Devices\" WHERE \"Name\" ILIKE @Filter OR \"Imei\" ILIKE @Filter OR \"SerialNumber\" ILIKE @Filter";
            
            var devices = await connection.QueryAsync<Device>(sql, new { Filter = $"%{filter}%", @PageSize = pageSize, @Offset = offset });
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { Filter = $"%{filter}%" });
            
            return (devices, totalCount);
        }

        public async Task<bool> BorrowDevicesAsync(int modelId, int quantity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var availableDevices = await connection.QueryAsync<int>(
                    "SELECT \"Id\" FROM public.\"Devices\" WHERE \"ModelId\" = @ModelId AND \"IsBorrowed\" = FALSE LIMIT @Quantity",
                    new { ModelId = modelId, Quantity = quantity },
                    transaction);

                if (availableDevices.Count() < quantity)
                    return false;

                await connection.ExecuteAsync(
                    "UPDATE public.\"Devices\" SET \"IsBorrowed\" = TRUE WHERE \"Id\" = ANY(@Ids)",
                    new { Ids = availableDevices.ToArray() },
                    transaction);

                await connection.ExecuteAsync(
                    "UPDATE public.\"Models\" SET \"Available\" = \"Available\" - @Quantity WHERE \"Id\" = @ModelId",
                    new { Quantity = quantity, ModelId = modelId },
                    transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
