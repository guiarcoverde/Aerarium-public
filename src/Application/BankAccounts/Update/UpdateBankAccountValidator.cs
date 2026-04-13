namespace Aerarium.Application.BankAccounts.Update;

using FluentValidation;

public sealed class UpdateBankAccountValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
