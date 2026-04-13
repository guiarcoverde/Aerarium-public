namespace Aerarium.UnitTests.Domain.Entities;

using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;
using Aerarium.Infrastructure.Services;
using FluentAssertions;

public sealed class TransactionTests
{
    private const string ValidUserId = "user-123";
    private const decimal ValidAmount = 150.50m;
    private const string ValidDescription = "Test transaction";
    private static readonly DateOnly ValidDate = new(2026, 4, 1);
    private static readonly Guid ValidBankAccountId = Guid.CreateVersion7();
    private static readonly Guid ValidCardId = Guid.CreateVersion7();

    [Fact]
    public void Create_ExpenseWithoutPaymentMethod_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Payment method is required");
    }

    [Fact]
    public void Create_ExpenseWithPaymentMethod_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Credit, cardId: ValidCardId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentMethod.Should().Be(PaymentMethod.Credit);
        result.Value.CardId.Should().Be(ValidCardId);
    }

    [Fact]
    public void Create_IncomeWithPaymentMethod_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            paymentMethod: PaymentMethod.Pix, bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not allowed");
    }

    [Fact]
    public void Create_ValidIncome_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(ValidAmount);
        result.Value.Description.Should().Be(ValidDescription);
        result.Value.Date.Should().Be(ValidDate);
        result.Value.Type.Should().Be(TransactionType.Income);
        result.Value.Category.Should().Be(TransactionCategory.Salary);
        result.Value.UserId.Should().Be(ValidUserId);
        result.Value.BankAccountId.Should().Be(ValidBankAccountId);
        result.Value.CardId.Should().BeNull();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ZeroAmount_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, 0, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Amount");
    }

    [Fact]
    public void Create_NegativeAmount_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, -10m, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Amount");
    }

    [Fact]
    public void Create_EmptyDescription_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, "", ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Description");
    }

    [Fact]
    public void Create_IncomeCategoryWithExpenseType_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Salary,
            paymentMethod: PaymentMethod.Pix, bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Category");
    }

    [Fact]
    public void Create_ExpenseCategoryWithIncomeType_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Housing,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Category");
    }

    [Fact]
    public void Update_ValidInputs_UpdatesPropertiesAndSetsUpdatedAt()
    {
        var transaction = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId).Value!;

        var newDate = new DateOnly(2026, 5, 1);
        var result = transaction.Update(
            200m, "Updated description", newDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Pix,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(200m);
        result.Value.Description.Should().Be("Updated description");
        result.Value.Date.Should().Be(newDate);
        result.Value.Type.Should().Be(TransactionType.Expense);
        result.Value.Category.Should().Be(TransactionCategory.Housing);
        result.Value.BankAccountId.Should().Be(ValidBankAccountId);
        result.Value.UpdatedAt.Should().NotBeNull();
        result.Value.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_RecurringWithEndDate_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly, recurrenceEndDate: new DateOnly(2026, 12, 31),
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Recurrence.Should().Be(Recurrence.Monthly);
        result.Value.RecurrenceGroupId.Should().NotBeNull();
        result.Value.RecurrenceEndDate.Should().Be(new DateOnly(2026, 12, 31));
        result.Value.RecurrenceCount.Should().BeNull();
    }

    [Fact]
    public void Create_RecurringWithoutEndDateOrCount_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Weekly,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("end date or an occurrence count");
    }

    [Fact]
    public void CreateSeries_Monthly_GeneratesCorrectOccurrences()
    {
        var result = Transaction.CreateSeries(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly, recurrenceEndDate: null, recurrenceCount: 3,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var series = result.Value!;
        series.Should().HaveCount(3);
        series[0].Date.Should().Be(new DateOnly(2026, 4, 1));
        series[1].Date.Should().Be(new DateOnly(2026, 5, 1));
        series[2].Date.Should().Be(new DateOnly(2026, 6, 1));
        series.Should().AllSatisfy(t => t.RecurrenceGroupId.Should().Be(series[0].RecurrenceGroupId));
        series.Should().AllSatisfy(t => t.BankAccountId.Should().Be(ValidBankAccountId));
    }

    [Fact]
    public void CreateSeries_EndDate_StopsAtEndDate()
    {
        var result = Transaction.CreateSeries(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly, recurrenceEndDate: new DateOnly(2026, 6, 15), recurrenceCount: null,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var series = result.Value!;
        series.Should().HaveCount(3);
        series.Last().Date.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public void CreateSeries_SalaryBusinessDay_UsesCorrectDates()
    {
        var calendar = new BrazilianBusinessDayCalendar();
        var schedule = SalarySchedule.Create(SalaryScheduleMode.BusinessDay, businessDayNumber: 5).Value!;

        var result = Transaction.CreateSeries(
            ValidUserId, 5000m, "Salary", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly, recurrenceEndDate: null, recurrenceCount: 3,
            salarySchedule: schedule, calendar: calendar,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var series = result.Value!;
        series.Should().HaveCount(3);
        series[0].Date.Should().Be(calendar.GetNthBusinessDay(2026, 4, 5));
        series[1].Date.Should().Be(calendar.GetNthBusinessDay(2026, 5, 5));
        series[2].Date.Should().Be(calendar.GetNthBusinessDay(2026, 6, 5));
    }

    [Fact]
    public void CreateSeries_SalarySplitDate_CreatesTwoPerMonth()
    {
        var calendar = new BrazilianBusinessDayCalendar();
        var schedule = SalarySchedule.Create(
            SalaryScheduleMode.FixedDateSplit, fixedDay: 15,
            splitFirstPercentage: 40m).Value!;

        var result = Transaction.CreateSeries(
            ValidUserId, 10000m, "Salary", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            Recurrence.Monthly, recurrenceEndDate: null, recurrenceCount: 2,
            salarySchedule: schedule, calendar: calendar,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var series = result.Value!;
        series.Should().HaveCount(4);
        series[0].Amount.Should().Be(4000m);
        series[1].Amount.Should().Be(6000m);
        series[2].Amount.Should().Be(4000m);
        series[3].Amount.Should().Be(6000m);
    }

    [Fact]
    public void Create_SalaryScheduleOnNonSalaryCategory_ReturnsFailure()
    {
        var schedule = SalarySchedule.Create(SalaryScheduleMode.BusinessDay, businessDayNumber: 5).Value!;

        var result = Transaction.Create(
            ValidUserId, 1000m, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Bonus,
            Recurrence.Monthly, recurrenceCount: 3,
            salarySchedule: schedule,
            bankAccountId: ValidBankAccountId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Salary category");
    }

    // --- Wallet source validation tests ---

    [Fact]
    public void Create_IncomeWithoutBankAccount_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Bank account is required for income");
    }

    [Fact]
    public void Create_IncomeWithCard_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Income, TransactionCategory.Salary,
            bankAccountId: ValidBankAccountId, cardId: ValidCardId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Card is not allowed for income");
    }

    [Fact]
    public void Create_ExpensePixWithoutBank_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Pix);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Bank account is required for Pix");
    }

    [Fact]
    public void Create_ExpensePixWithCard_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Pix,
            bankAccountId: ValidBankAccountId, cardId: ValidCardId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Card is not allowed for Pix");
    }

    [Fact]
    public void Create_ExpensePixWithBank_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Pix,
            bankAccountId: ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BankAccountId.Should().Be(ValidBankAccountId);
        result.Value.CardId.Should().BeNull();
    }

    [Fact]
    public void Create_ExpenseDebitWithoutCard_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Debit);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Card is required for debit");
    }

    [Fact]
    public void Create_ExpenseDebitWithCard_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Debit, cardId: ValidCardId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CardId.Should().Be(ValidCardId);
    }

    [Fact]
    public void Create_ExpenseCreditWithoutCard_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Credit);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Card is required");
    }

    [Fact]
    public void Create_ExpenseCreditWithCard_ReturnsSuccess()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Credit, cardId: ValidCardId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CardId.Should().Be(ValidCardId);
    }

    [Fact]
    public void Create_ExpenseCardWithBankAccount_ReturnsFailure()
    {
        var result = Transaction.Create(
            ValidUserId, ValidAmount, ValidDescription, ValidDate,
            TransactionType.Expense, TransactionCategory.Housing,
            paymentMethod: PaymentMethod.Credit,
            bankAccountId: ValidBankAccountId, cardId: ValidCardId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Bank account must not be set directly for card expenses");
    }
}
