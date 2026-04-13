namespace Aerarium.Application.Investments.Create;

using FluentValidation;

public sealed class CreateInvestmentValidator : AbstractValidator<CreateInvestmentCommand>
{
    public CreateInvestmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrentValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Type).IsInEnum();
    }
}
