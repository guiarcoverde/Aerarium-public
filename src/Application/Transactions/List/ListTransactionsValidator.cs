namespace Aerarium.Application.Transactions.List;

using FluentValidation;

public sealed class ListTransactionsValidator : AbstractValidator<ListTransactionsQuery>
{
    public ListTransactionsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue);

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .When(x => x.Year.HasValue);

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);
    }
}

