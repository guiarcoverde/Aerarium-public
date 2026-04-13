namespace Aerarium.Application.BankAccounts.Update;

using Aerarium.Application.BankAccounts.Create;
using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateBankAccountHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<UpdateBankAccountCommand, Result<BankAccountDto>>
{
    public async ValueTask<Result<BankAccountDto>> Handle(
        UpdateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.BankAccounts
            .FirstOrDefaultAsync(b => b.Id == command.Id && b.UserId == currentUser.UserId, cancellationToken);

        if (account is null)
            return Result<BankAccountDto>.Failure("Bank account not found.");

        var result = account.Rename(command.Name);

        if (result.IsFailure)
            return Result<BankAccountDto>.Failure(result.Error!);

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateBankAccountHandler.ToDto(account);
    }
}
