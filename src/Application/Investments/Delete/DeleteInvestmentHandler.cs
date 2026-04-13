namespace Aerarium.Application.Investments.Delete;

using Aerarium.Application.Common;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteInvestmentHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : ICommandHandler<DeleteInvestmentCommand, Result<bool>>
{
    public async ValueTask<Result<bool>> Handle(
        DeleteInvestmentCommand command,
        CancellationToken cancellationToken)
    {
        var investment = await dbContext.Investments
            .FirstOrDefaultAsync(i => i.Id == command.Id && i.UserId == currentUser.UserId, cancellationToken);

        if (investment is null)
            return Result<bool>.Failure("Investment not found.");

        dbContext.Investments.Remove(investment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
