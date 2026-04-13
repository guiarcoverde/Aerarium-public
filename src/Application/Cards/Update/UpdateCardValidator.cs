namespace Aerarium.Application.Cards.Update;

using Aerarium.Domain.Enums;
using FluentValidation;

public sealed class UpdateCardValidator : AbstractValidator<UpdateCardCommand>
{
    public UpdateCardValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).Must(t => (t & (CardType.Debit | CardType.Credit)) != 0)
            .WithMessage("Card must be debit, credit, or both.");
        RuleFor(x => x.CreditLimit).GreaterThan(0)
            .When(x => x.Type.HasFlag(CardType.Credit));
        RuleFor(x => x.LinkedBankAccountId).NotEmpty()
            .When(x => x.Type.HasFlag(CardType.Debit))
            .WithMessage("A bank account is required for debit cards.");
    }
}
