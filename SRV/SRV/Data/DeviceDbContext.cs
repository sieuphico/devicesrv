using Microsoft.EntityFrameworkCore;

namespace SRV.Data
{
    public class DeviceDbContext : DbContext
    {
        public DbSet<Model> Models { get; set; }
        public DbSet<Device> Devices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=DeviceSrvDb;Username=postgres;Password=root");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Yêu cầu sử dụng extension pg_trgm cho tìm kiếm văn bản LIKE (gin) hiệu quả ở PostgreSQL
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<Model>()
                .HasIndex(m => m.Name)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<Model>()
                .HasIndex(m => m.Category)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<Model>()
                .HasIndex(m => m.Subcategory)
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");

            modelBuilder.Entity<Model>()
                .HasMany(m => m.Devices)
                .WithOne(d => d.Model)
                .HasForeignKey(d => d.ModelId);
                
            base.OnModelCreating(modelBuilder);
        }
    }
}
