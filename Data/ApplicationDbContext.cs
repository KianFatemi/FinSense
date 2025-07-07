using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceDashboard.Models;

namespace PersonalFinanceDashboard.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<FinancialAccount> FinancialAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<PlaidItem> PlaidItems { get; set; }
        public DbSet<Security> Securities { get; set; }
        public DbSet<Holding> Holdings { get; set; }
    }
}
