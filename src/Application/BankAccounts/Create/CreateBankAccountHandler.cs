namespace Aerarium.Application.BankAccounts.Create;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Mediator;

public sealed class CreateBankAccountHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<CreateBankAccountCommand, Result<BankAccountDto>>
{
    public async ValueTask<Result<BankAccountDto>> Handle(
        CreateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = BankAccount.Create(
            currentUser.UserId,
            command.Name,
            command.Balance);

        if (result.IsFailure)
            return Result<BankAccountDto>.Failure(result.Error!);

        var account = result.Value!;
        dbContext.BankAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(account);
    }

    internal static BankAccountDto ToDto(BankAccount b) => new(
        b.Id, b.Name, b.Balance, b.CreatedAt, b.UpdatedAt);
}
