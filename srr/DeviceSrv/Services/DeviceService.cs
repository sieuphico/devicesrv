using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sql_debug.log");

        private void LogSql(string sql, object? parameters, long durationMs)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var paramStr = parameters != null ? System.Text.Json.JsonSerializer.Serialize(parameters) : "null";
                var logEntry = $"[{timestamp}] [{durationMs}ms] SQL: {sql} | Params: {paramStr}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);
            }
            catch
            {
                // Ignore logging errors to prevent app crash
            }
        }

        public async Task<(IEnumerable<Model> Models, int TotalCount)> GetModelsPagedAsync(int pageNumber, int pageSize, string orderBy = "Name", bool isDescending = false, string nameFilter = "", string manufacturerFilter = "", string categoryFilter = "All")
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;
            
            var validColumns = new[] { "Id", "Name", "Manufacturer", "Category", "Subcategory", "Available" };
            if (!validColumns.Contains(orderBy)) orderBy = "Name";
            var sortDir = isDescending ? "DESC" : "ASC";

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(nameFilter))
            {
                whereClauses.Add("\"Name\" ILIKE @NameFilter");
                parameters.Add("NameFilter", $"%{nameFilter}%");
            }
            if (!string.IsNullOrEmpty(manufacturerFilter))
            {
                whereClauses.Add("\"Manufacturer\" ILIKE @ManFilter");
                parameters.Add("ManFilter", $"%{manufacturerFilter}%");
            }
            if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All")
            {
                whereClauses.Add("\"Category\" = @CatFilter");
                parameters.Add("CatFilter", categoryFilter);
            }

            var whereSql = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            
            var sql = $"SELECT * FROM public.\"Models\" {whereSql} ORDER BY \"{orderBy}\" {sortDir} LIMIT @PageSize OFFSET @Offset";
            var countSql = $"SELECT COUNT(*) FROM public.\"Models\" {whereSql}";

            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", offset);

            var models = await connection.QueryAsync<Model>(sql, parameters);
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            
            sw.Stop();
            LogSql(sql, new { nameFilter, manufacturerFilter, categoryFilter, pageSize, offset }, sw.ElapsedMilliseconds);
            
            return (models, totalCount);
        }

        public async Task<IEnumerable<string>> GetDistinctCategoriesAsync()
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT DISTINCT \"Category\" FROM public.\"Models\" WHERE \"Category\" IS NOT NULL ORDER BY \"Category\"";
            var categories = await connection.QueryAsync<string>(sql);
            sw.Stop();
            LogSql(sql, null, sw.ElapsedMilliseconds);
            return categories;
        }

        public async Task<IEnumerable<string>> GetDistinctManufacturersAsync()
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT DISTINCT \"Manufacturer\" FROM public.\"Models\" WHERE \"Manufacturer\" IS NOT NULL ORDER BY \"Manufacturer\"";
            var manufacturers = await connection.QueryAsync<string>(sql);
            sw.Stop();
            LogSql(sql, null, sw.ElapsedMilliseconds);
            return manufacturers;
        }

        public async Task<(IEnumerable<Device> Devices, int TotalCount)> GetDevicesPagedAsync(int pageNumber, int pageSize, string orderBy = "Id", bool isDescending = false, string filter = "")
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;
            
            var validColumns = new[] { "Id", "Name", "Imei", "SerialNumber", "IsBorrowed" };
            if (!validColumns.Contains(orderBy)) orderBy = "Id";
            var sortDir = isDescending ? "DESC" : "ASC";

            var whereSql = "";
            var parameters = new DynamicParameters();
            if (!string.IsNullOrEmpty(filter))
            {
                // Optimization: Use separate parameters or trgm optimized query if available
                whereSql = "WHERE \"Name\" ILIKE @Filter OR \"Imei\" ILIKE @Filter OR \"SerialNumber\" ILIKE @Filter";
                parameters.Add("Filter", $"%{filter}%");
            }
            
            var sql = $"SELECT * FROM public.\"Devices\" {whereSql} ORDER BY \"{orderBy}\" {sortDir} LIMIT @PageSize OFFSET @Offset";
            var countSql = $"SELECT COUNT(*) FROM public.\"Devices\" {whereSql}";
            
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", offset);
            
            var devices = await connection.QueryAsync<Device>(sql, parameters);
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            
            sw.Stop();
            LogSql(sql, new { filter, pageSize, offset }, sw.ElapsedMilliseconds);
            
            return (devices, totalCount);
        }

        public async Task<bool> BorrowDevicesAsync(int modelId, int quantity)
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var sqlFetch = "SELECT \"Id\" FROM public.\"Devices\" WHERE \"ModelId\" = @ModelId AND \"IsBorrowed\" = FALSE LIMIT @Quantity";
                var availableDevices = await connection.QueryAsync<int>(
                    sqlFetch,
                    new { ModelId = modelId, Quantity = quantity },
                    transaction);

                if (availableDevices.Count() < quantity)
                    return false;

                var sqlUpdateDevices = "UPDATE public.\"Devices\" SET \"IsBorrowed\" = TRUE WHERE \"Id\" = ANY(@Ids)";
                await connection.ExecuteAsync(
                    sqlUpdateDevices,
                    new { Ids = availableDevices.ToArray() },
                    transaction);

                var sqlUpdateModel = "UPDATE public.\"Models\" SET \"Available\" = \"Available\" - @Quantity WHERE \"Id\" = @ModelId";
                await connection.ExecuteAsync(
                    sqlUpdateModel,
                    new { Quantity = quantity, ModelId = modelId },
                    transaction);

                await transaction.CommitAsync();
                sw.Stop();
                LogSql("BorrowDevices Transaction", new { modelId, quantity, deviceCount = availableDevices.Count() }, sw.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                LogSql("BorrowDevices Transaction FAILED", new { modelId, quantity, Error = ex.Message }, sw.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<bool> ToggleDeviceBorrowStatusAsync(int deviceId)
        {
            var sw = Stopwatch.StartNew();
            using var connection = new NpgsqlConnection(_connectionString);
            
            // Single SQL statement using CTE for maximum performance (Atomic at Database level)
            var sql = @"
                WITH updated_device AS (
                    UPDATE public.""Devices""
                    SET ""IsBorrowed"" = NOT ""IsBorrowed""
                    WHERE ""Id"" = @Id
                    RETURNING ""ModelId"", ""IsBorrowed""
                )
                UPDATE public.""Models"" m
                SET ""Available"" = m.""Available"" + (CASE WHEN d.""IsBorrowed"" THEN -1 ELSE 1 END)
                FROM updated_device d
                WHERE m.""Id"" = d.""ModelId""
                RETURNING d.""IsBorrowed"";";

            try
            {
                var newStatus = await connection.ExecuteScalarAsync<bool>(sql, new { Id = deviceId });
                sw.Stop();
                LogSql("ToggleDeviceBorrowStatus (Optimized CTE)", new { deviceId, newStatus }, sw.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                LogSql("ToggleDeviceBorrowStatus (Optimized CTE) FAILED", new { deviceId, Error = ex.Message }, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
