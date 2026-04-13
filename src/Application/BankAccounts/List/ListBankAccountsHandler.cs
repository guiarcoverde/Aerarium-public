namespace Aerarium.Application.BankAccounts.List;

using Aerarium.Application.BankAccounts.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class ListBankAccountsHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<ListBankAccountsQuery, Result<IReadOnlyList<BankAccountDto>>>
{
    public async ValueTask<Result<IReadOnlyList<BankAccountDto>>> Handle(
        ListBankAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.BankAccounts
            .AsNoTracking()
            .Where(b => b.UserId == currentUser.UserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return accounts.Select(CreateBankAccountHandler.ToDto).ToList();
    }
}
