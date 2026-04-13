namespace Aerarium.UnitTests.Application.Transactions;

using Aerarium.Application.Transactions.Create;
using Aerarium.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

public sealed class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();
    private static readonly Guid ValidBankAccountId = Guid.CreateVersion7();
    private static readonly Guid ValidCardId = Guid.CreateVersion7();

    [Fact]
    public void Validate_ValidIncomeCommand_NoErrors()
    {
        var command = new CreateTransactionCommand(
            100m, "Salary payment", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidExpensePixCommand_NoErrors()
    {
        var command = new CreateTransactionCommand(
            100m, "Transfer", new DateOnly(2026, 4, 1),
            TransactionType.Expense, TransactionCategory.Housing,
            PaymentMethod: PaymentMethod.Pix,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidExpenseCreditCommand_NoErrors()
    {
        var command = new CreateTransactionCommand(
            100m, "Purchase", new DateOnly(2026, 4, 1),
            TransactionType.Expense, TransactionCategory.Housing,
            PaymentMethod: PaymentMethod.Credit,
            CardId: ValidCardId);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroAmount_HasError()
    {
        var command = new CreateTransactionCommand(
            0, "Test", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_EmptyDescription_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_InvalidCategoryForType_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Test", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Housing,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Validate_RecurringWithoutEndRule_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Test", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly,
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Recurring transactions must have either an end date or an occurrence count.");
    }

    [Fact]
    public void Validate_NonRecurringWithEndDate_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Test", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.None, RecurrenceEndDate: new DateOnly(2026, 12, 31),
            BankAccountId: ValidBankAccountId);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecurrenceEndDate);
    }

    [Fact]
    public void Validate_IncomeWithoutBankAccount_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Salary", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BankAccountId);
    }

    [Fact]
    public void Validate_IncomeWithCard_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Salary", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            BankAccountId: ValidBankAccountId,
            CardId: ValidCardId);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CardId);
    }

    [Fact]
    public void Validate_ExpensePixWithoutBankAccount_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Pix transfer", new DateOnly(2026, 4, 1),
            TransactionType.Expense, TransactionCategory.Housing,
            PaymentMethod: PaymentMethod.Pix);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.BankAccountId);
    }

    [Fact]
    public void Validate_ExpenseCreditWithoutCard_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Purchase", new DateOnly(2026, 4, 1),
            TransactionType.Expense, TransactionCategory.Housing,
            PaymentMethod: PaymentMethod.Credit);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CardId);
    }

    [Fact]
    public void Validate_ExpenseDebitWithoutCard_HasError()
    {
        var command = new CreateTransactionCommand(
            100m, "Purchase", new DateOnly(2026, 4, 1),
            TransactionType.Expense, TransactionCategory.Housing,
            PaymentMethod: PaymentMethod.Debit);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CardId);
    }
}
