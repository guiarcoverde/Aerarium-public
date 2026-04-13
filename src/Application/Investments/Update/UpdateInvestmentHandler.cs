namespace Aerarium.Application.Investments.Update;

using Aerarium.Application.Common;
using Aerarium.Application.Investments.Create;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateInvestmentHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<UpdateInvestmentCommand, Result<InvestmentDto>>
{
    public async ValueTask<Result<InvestmentDto>> Handle(
        UpdateInvestmentCommand command,
        CancellationToken cancellationToken)
    {
        var investment = await dbContext.Investments
            .FirstOrDefaultAsync(i => i.Id == command.Id && i.UserId == currentUser.UserId, cancellationToken);

        if (investment is null)
            return Result<InvestmentDto>.Failure("Investment not found.");

        var result = investment.Update(
            command.Name, command.Amount, command.CurrentValue, command.Type, command.PurchaseDate);

        if (result.IsFailure)
            return Result<InvestmentDto>.Failure(result.Error!);

        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateInvestmentHandler.ToDto(investment);
    }
}
