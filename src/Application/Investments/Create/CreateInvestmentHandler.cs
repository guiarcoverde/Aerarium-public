namespace Aerarium.Application.Investments.Create;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Mediator;

public sealed class CreateInvestmentHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<CreateInvestmentCommand, Result<InvestmentDto>>
{
    public async ValueTask<Result<InvestmentDto>> Handle(
        CreateInvestmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = Investment.Create(
            currentUser.UserId,
            command.Name,
            command.Amount,
            command.CurrentValue,
            command.Type,
            command.PurchaseDate);

        if (result.IsFailure)
            return Result<InvestmentDto>.Failure(result.Error!);

        var investment = result.Value!;
        dbContext.Investments.Add(investment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(investment);
    }

    internal static InvestmentDto ToDto(Investment i) => new(
        i.Id, i.Name, i.Amount, i.CurrentValue, i.Type, i.PurchaseDate, i.CreatedAt, i.UpdatedAt);
}
