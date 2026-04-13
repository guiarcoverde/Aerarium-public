namespace Aerarium.Application.Transactions.Update;

using Aerarium.Domain.Enums;
using FluentValidation;

public sealed class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.Category)
            .Must((cmd, category) => IsValidCategoryForType(cmd.Type, category))
            .WithMessage("Category is not valid for the given transaction type.");

        RuleFor(x => x.Recurrence)
            .IsInEnum();

        When(x => x.Recurrence != Recurrence.None, () =>
        {
            RuleFor(x => x)
                .Must(x => x.RecurrenceEndDate is not null || x.RecurrenceCount is not null)
                .WithMessage("Recurring transactions must have either an end date or an occurrence count.");
        });

        When(x => x.Recurrence == Recurrence.None, () =>
        {
            RuleFor(x => x.RecurrenceEndDate).Null()
                .WithMessage("Non-recurring transactions cannot have an end date.");
            RuleFor(x => x.RecurrenceCount).Null()
                .WithMessage("Non-recurring transactions cannot have an occurrence count.");
        });

        RuleFor(x => x.RecurrenceCount)
            .GreaterThan(0)
            .When(x => x.RecurrenceCount is not null);

        RuleFor(x => x.PaymentMethod)
            .NotNull()
            .IsInEnum()
            .When(x => x.Type == TransactionType.Expense)
            .WithMessage("Payment method is required for expense transactions.");

        RuleFor(x => x.PaymentMethod)
            .Null()
            .When(x => x.Type == TransactionType.Income)
            .WithMessage("Payment method is not allowed for income transactions.");

        RuleFor(x => x.BankAccountId)
            .NotEmpty()
            .When(x => x.Type == TransactionType.Income)
            .WithMessage("Bank account is required for income transactions.");

        RuleFor(x => x.CardId)
            .Null()
            .When(x => x.Type == TransactionType.Income)
            .WithMessage("Card is not allowed for income transactions.");

        RuleFor(x => x.BankAccountId)
            .NotEmpty()
            .When(x => x.Type == TransactionType.Expense && x.PaymentMethod == PaymentMethod.Pix)
            .WithMessage("Bank account is required for Pix expenses.");

        RuleFor(x => x.CardId)
            .NotEmpty()
            .When(x => x.Type == TransactionType.Expense && x.PaymentMethod is PaymentMethod.Debit or PaymentMethod.Credit)
            .WithMessage("Card is required for debit and credit expenses.");
    }

    private static bool IsValidCategoryForType(TransactionType type, TransactionCategory category)
    {
        return type switch
        {
            TransactionType.Income => (int)category is >= 100 and < 200,
            TransactionType.Expense => (int)category is >= 200 and < 300,
            _ => false
        };
    }
}
