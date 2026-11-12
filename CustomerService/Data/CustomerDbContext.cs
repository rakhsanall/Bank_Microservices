using CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Data
{
    public class CustomerDbContext : DbContext
    {
        public CustomerDbContext(
            DbContextOptions<CustomerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(x => x.Email).IsUnique();
            });
        }
    }
}