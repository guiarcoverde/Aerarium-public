namespace Aerarium.Application.Investments.Update;

using FluentValidation;

public sealed class UpdateInvestmentValidator : AbstractValidator<UpdateInvestmentCommand>
{
    public UpdateInvestmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrentValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Type).IsInEnum();
    }
}
