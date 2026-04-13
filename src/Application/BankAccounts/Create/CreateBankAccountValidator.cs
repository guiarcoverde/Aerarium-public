namespace Aerarium.Application.BankAccounts.Create;

using FluentValidation;

public sealed class CreateBankAccountValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
