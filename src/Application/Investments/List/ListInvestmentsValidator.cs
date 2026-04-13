namespace Aerarium.Application.Investments.List;

using FluentValidation;

public sealed class ListInvestmentsValidator : AbstractValidator<ListInvestmentsQuery>
{
    public ListInvestmentsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
