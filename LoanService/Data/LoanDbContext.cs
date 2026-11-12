using LoanService.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Data
{
    public class LoanDbContext : DbContext
    {
        public LoanDbContext(DbContextOptions<LoanDbContext> options)
            : base(options)
        {
        }

        public DbSet<LoanDocument> LoanDocuments => Set<LoanDocument>();
    }
}