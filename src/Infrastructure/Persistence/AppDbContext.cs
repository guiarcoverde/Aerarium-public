namespace Aerarium.Infrastructure.Persistence;

using Aerarium.Application.Common;
using Aerarium.Domain.Entities;
using Aerarium.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IAppDbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
