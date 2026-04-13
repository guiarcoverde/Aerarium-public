namespace Aerarium.Application.Investments.GetById;

using Aerarium.Application.Common;
using Aerarium.Application.Investments.Create;
using Aerarium.Domain.Common;
using Mediator;
using Microsoft.EntityFrameworkCore;

public sealed class GetInvestmentHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUser) : IQueryHandler<GetInvestmentQuery, Result<InvestmentDto>>
{
    public async ValueTask<Result<InvestmentDto>> Handle(
        GetInvestmentQuery query,
        CancellationToken cancellationToken)
    {
        var investment = await dbContext.Investments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == query.Id && i.UserId == currentUser.UserId, cancellationToken);

        if (investment is null)
            return Result<InvestmentDto>.Failure("Investment not found.");

        return CreateInvestmentHandler.ToDto(investment);
    }
}
