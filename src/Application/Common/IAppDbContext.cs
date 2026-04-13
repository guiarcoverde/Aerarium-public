namespace Aerarium.Application.Common;

using Aerarium.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IAppDbContext
{
    DbSet<Transaction> Transactions { get; }
    DbSet<Investment> Investments { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<Card> Cards { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
