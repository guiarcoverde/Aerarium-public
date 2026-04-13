namespace Aerarium.Application.BankAccounts.GetById;

using Aerarium.Application.BankAccounts.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class GetBankAccountHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<GetBankAccountQuery, Result<BankAccountDto>>
{
    public async ValueTask<Result<BankAccountDto>> Handle(
        GetBankAccountQuery query,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.BankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == query.Id && b.UserId == currentUser.UserId, cancellationToken);

        if (account is null)
            return Result<BankAccountDto>.Failure("Bank account not found.");

        return CreateBankAccountHandler.ToDto(account);
    }
}
