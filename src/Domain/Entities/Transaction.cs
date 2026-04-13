namespace Aerarium.Domain.Entities;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionCategory Category { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public Recurrence Recurrence { get; private set; }
    public Guid? RecurrenceGroupId { get; private set; }
    public DateOnly? RecurrenceEndDate { get; private set; }
    public int? RecurrenceCount { get; private set; }
    public SalarySchedule? SalarySchedule { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public Guid? CardId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Transaction() { }

    public static Result<Transaction> Create(
        string userId,
        decimal amount,
        string description,
        DateOnly date,
        TransactionType type,
        TransactionCategory category,
        Recurrence recurrence = Recurrence.None,
        DateOnly? recurrenceEndDate = null,
        int? recurrenceCount = null,
        SalarySchedule? salarySchedule = null,
        PaymentMethod? paymentMethod = null,
        Guid? bankAccountId = null,
        Guid? cardId = null)
    {
        if (amount <= 0)
            return Result<Transaction>.Failure("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            return Result<Transaction>.Failure("Description is required.");

        if (!IsValidCategoryForType(type, category))
            return Result<Transaction>.Failure("Category is not valid for the given transaction type.");

        var paymentValidation = ValidatePaymentMethod(type, paymentMethod);
        if (paymentValidation is not null)
            return Result<Transaction>.Failure(paymentValidation);

        var walletValidation = ValidateWalletSource(type, paymentMethod, bankAccountId, cardId);
        if (walletValidation is not null)
            return Result<Transaction>.Failure(walletValidation);

        var recurrenceValidation = ValidateRecurrence(recurrence, recurrenceEndDate, recurrenceCount);
        if (recurrenceValidation is not null)
            return Result<Transaction>.Failure(recurrenceValidation);

        var salaryValidation = ValidateSalarySchedule(salarySchedule, category, recurrence);
        if (salaryValidation is not null)
            return Result<Transaction>.Failure(salaryValidation);

        return new Transaction
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Amount = amount,
            Description = description.Trim(),
            Date = date,
            Type = type,
            Category = category,
            PaymentMethod = paymentMethod,
            BankAccountId = bankAccountId,
            CardId = cardId,
            Recurrence = recurrence,
            RecurrenceGroupId = recurrence != Recurrence.None ? Guid.CreateVersion7() : null,
            RecurrenceEndDate = recurrence != Recurrence.None ? recurrenceEndDate : null,
            RecurrenceCount = recurrence != Recurrence.None ? recurrenceCount : null,
            SalarySchedule = salarySchedule,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string? ValidatePaymentMethod(TransactionType type, PaymentMethod? paymentMethod)
    {
        return type switch
        {
            TransactionType.Expense when paymentMethod is null => "Payment method is required for expense transactions.",
            TransactionType.Income when paymentMethod is not null => "Payment method is not allowed for income transactions.",
            _ => null
        };
    }

    private static string? ValidateWalletSource(
        TransactionType type,
        PaymentMethod? paymentMethod,
        Guid? bankAccountId,
        Guid? cardId)
    {
        if (type == TransactionType.Income)
        {
            if (bankAccountId is null)
                return "Bank account is required for income transactions.";
            if (cardId is not null)
                return "Card is not allowed for income transactions.";
            return null;
        }

        // Expense
        if (paymentMethod == Enums.PaymentMethod.Pix)
        {
            if (bankAccountId is null)
                return "Bank account is required for Pix expenses.";
            if (cardId is not null)
                return "Card is not allowed for Pix expenses.";
            return null;
        }

        if (paymentMethod is Enums.PaymentMethod.Debit or Enums.PaymentMethod.Credit)
        {
            if (cardId is null)
                return "Card is required for debit and credit expenses.";
            if (bankAccountId is not null)
                return "Bank account must not be set directly for card expenses.";
            return null;
        }

        return null;
    }

    public static Result<IReadOnlyList<Transaction>> CreateSeries(
        string userId,
        decimal amount,
        string description,
        DateOnly startDate,
        TransactionType type,
        TransactionCategory category,
        Recurrence recurrence,
        DateOnly? recurrenceEndDate,
        int? recurrenceCount,
        SalarySchedule? salarySchedule = null,
        IBusinessDayCalendar? calendar = null,
        PaymentMethod? paymentMethod = null,
        Guid? bankAccountId = null,
        Guid? cardId = null)
    {
        if (salarySchedule is not null && calendar is null)
            return Result<IReadOnlyList<Transaction>>.Failure("Business day calendar is required for salary schedule.");

        var firstResult = Create(userId, amount, description, startDate, type, category,
            recurrence, recurrenceEndDate, recurrenceCount, salarySchedule, paymentMethod, bankAccountId, cardId);

        if (firstResult.IsFailure)
            return Result<IReadOnlyList<Transaction>>.Failure(firstResult.Error!);

        var first = firstResult.Value!;

        if (salarySchedule is not null)
            return CreateSalarySeries(first, userId, amount, description, type, category,
                recurrence, recurrenceEndDate, recurrenceCount, salarySchedule, calendar!, paymentMethod, bankAccountId, cardId);

        return CreateStandardSeries(first, userId, amount, description, type, category,
            recurrence, recurrenceEndDate, recurrenceCount, salarySchedule, startDate, paymentMethod, bankAccountId, cardId);
    }

    public Result<Transaction> Update(
        decimal amount,
        string description,
        DateOnly date,
        TransactionType type,
        TransactionCategory category,
        Recurrence recurrence = Recurrence.None,
        DateOnly? recurrenceEndDate = null,
        int? recurrenceCount = null,
        SalarySchedule? salarySchedule = null,
        PaymentMethod? paymentMethod = null,
        Guid? bankAccountId = null,
        Guid? cardId = null)
    {
        if (amount <= 0)
            return Result<Transaction>.Failure("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            return Result<Transaction>.Failure("Description is required.");

        if (!IsValidCategoryForType(type, category))
            return Result<Transaction>.Failure("Category is not valid for the given transaction type.");

        var paymentValidation = ValidatePaymentMethod(type, paymentMethod);
        if (paymentValidation is not null)
            return Result<Transaction>.Failure(paymentValidation);

        var walletValidation = ValidateWalletSource(type, paymentMethod, bankAccountId, cardId);
        if (walletValidation is not null)
            return Result<Transaction>.Failure(walletValidation);

        var recurrenceValidation = ValidateRecurrence(recurrence, recurrenceEndDate, recurrenceCount);
        if (recurrenceValidation is not null)
            return Result<Transaction>.Failure(recurrenceValidation);

        var salaryValidation = ValidateSalarySchedule(salarySchedule, category, recurrence);
        if (salaryValidation is not null)
            return Result<Transaction>.Failure(salaryValidation);

        Amount = amount;
        Description = description.Trim();
        Date = date;
        Type = type;
        Category = category;
        PaymentMethod = paymentMethod;
        BankAccountId = bankAccountId;
        CardId = cardId;
        Recurrence = recurrence;
        RecurrenceEndDate = recurrence != Recurrence.None ? recurrenceEndDate : null;
        RecurrenceCount = recurrence != Recurrence.None ? recurrenceCount : null;
        SalarySchedule = salarySchedule;
        UpdatedAt = DateTime.UtcNow;

        return this;
    }

    private static Result<IReadOnlyList<Transaction>> CreateStandardSeries(
        Transaction first,
        string userId, decimal amount, string description,
        TransactionType type, TransactionCategory category,
        Recurrence recurrence, DateOnly? recurrenceEndDate, int? recurrenceCount,
        SalarySchedule? salarySchedule, DateOnly startDate, PaymentMethod? paymentMethod,
        Guid? bankAccountId, Guid? cardId)
    {
        var transactions = new List<Transaction> { first };
        var currentDate = startDate;
        var maxOccurrences = recurrenceCount ?? 1000;

        for (var i = 1; i < maxOccurrences; i++)
        {
            currentDate = AdvanceDate(currentDate, recurrence);

            if (recurrenceEndDate.HasValue && currentDate > recurrenceEndDate.Value)
                break;

            transactions.Add(new Transaction
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Amount = amount,
                Description = description.Trim(),
                Date = currentDate,
                Type = type,
                Category = category,
                PaymentMethod = paymentMethod,
                BankAccountId = bankAccountId,
                CardId = cardId,
                Recurrence = recurrence,
                RecurrenceGroupId = first.RecurrenceGroupId,
                RecurrenceEndDate = recurrenceEndDate,
                RecurrenceCount = recurrenceCount,
                SalarySchedule = salarySchedule,
                CreatedAt = DateTime.UtcNow
            });
        }

        return transactions;
    }

    private static Result<IReadOnlyList<Transaction>> CreateSalarySeries(
        Transaction first,
        string userId, decimal totalAmount, string description,
        TransactionType type, TransactionCategory category,
        Recurrence recurrence, DateOnly? recurrenceEndDate, int? recurrenceCount,
        SalarySchedule schedule, IBusinessDayCalendar calendar, PaymentMethod? paymentMethod,
        Guid? bankAccountId, Guid? cardId)
    {
        var transactions = new List<Transaction>();
        var startMonth = first.Date.Month;
        var startYear = first.Date.Year;
        var maxMonths = recurrenceCount ?? 1000;

        for (var i = 0; i < maxMonths; i++)
        {
            var year = startYear + (startMonth + i - 1) / 12;
            var month = (startMonth + i - 1) % 12 + 1;

            if (recurrenceEndDate.HasValue && new DateOnly(year, month, 1) > recurrenceEndDate.Value)
                break;

            var groupId = first.RecurrenceGroupId;

            switch (schedule.Mode)
            {
                case SalaryScheduleMode.BusinessDay:
                {
                    var date = calendar.GetNthBusinessDay(year, month, schedule.BusinessDayNumber!.Value);
                    transactions.Add(CreateOccurrence(userId, totalAmount, description, date,
                        type, category, recurrence, recurrenceEndDate, recurrenceCount, schedule, groupId, paymentMethod, bankAccountId, cardId));
                    break;
                }

                case SalaryScheduleMode.FixedDate:
                {
                    var day = Math.Min(schedule.FixedDay!.Value, DateTime.DaysInMonth(year, month));
                    var targetDate = new DateOnly(year, month, day);
                    var date = calendar.GetPreviousBusinessDay(targetDate);
                    transactions.Add(CreateOccurrence(userId, totalAmount, description, date,
                        type, category, recurrence, recurrenceEndDate, recurrenceCount, schedule, groupId, paymentMethod, bankAccountId, cardId));
                    break;
                }

                case SalaryScheduleMode.FixedDateSplit:
                {
                    var (firstAmount, secondAmount) = CalculateSplit(totalAmount, schedule);

                    var day = Math.Min(schedule.FixedDay!.Value, DateTime.DaysInMonth(year, month));
                    var firstDate = calendar.GetPreviousBusinessDay(new DateOnly(year, month, day));
                    var secondDate = calendar.GetLastBusinessDay(year, month);

                    transactions.Add(CreateOccurrence(userId, firstAmount, description, firstDate,
                        type, category, recurrence, recurrenceEndDate, recurrenceCount, schedule, groupId, paymentMethod, bankAccountId, cardId));
                    transactions.Add(CreateOccurrence(userId, secondAmount, description, secondDate,
                        type, category, recurrence, recurrenceEndDate, recurrenceCount, schedule, groupId, paymentMethod, bankAccountId, cardId));
                    break;
                }
            }
        }

        return transactions;
    }

    private static Transaction CreateOccurrence(
        string userId, decimal amount, string description, DateOnly date,
        TransactionType type, TransactionCategory category,
        Recurrence recurrence, DateOnly? recurrenceEndDate, int? recurrenceCount,
        SalarySchedule? salarySchedule, Guid? recurrenceGroupId, PaymentMethod? paymentMethod,
        Guid? bankAccountId, Guid? cardId)
    {
        return new Transaction
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Amount = amount,
            Description = description.Trim(),
            Date = date,
            Type = type,
            Category = category,
            PaymentMethod = paymentMethod,
            BankAccountId = bankAccountId,
            CardId = cardId,
            Recurrence = recurrence,
            RecurrenceGroupId = recurrenceGroupId,
            RecurrenceEndDate = recurrenceEndDate,
            RecurrenceCount = recurrenceCount,
            SalarySchedule = salarySchedule,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static (decimal firstAmount, decimal secondAmount) CalculateSplit(decimal totalAmount, SalarySchedule schedule)
    {
        if (schedule.SplitFirstAmount.HasValue)
        {
            var firstAmount = schedule.SplitFirstAmount.Value;
            return (firstAmount, totalAmount - firstAmount);
        }

        var firstByPercentage = Math.Round(totalAmount * schedule.SplitFirstPercentage!.Value / 100m, 2);
        return (firstByPercentage, totalAmount - firstByPercentage);
    }

    private static string? ValidateSalarySchedule(SalarySchedule? schedule, TransactionCategory category, Recurrence recurrence)
    {
        if (schedule is null)
            return null;

        if (category != TransactionCategory.Salary)
            return "Salary schedule is only valid for Salary category transactions.";

        if (recurrence != Recurrence.Monthly)
            return "Salary schedule requires monthly recurrence.";

        return null;
    }

    private static string? ValidateRecurrence(Recurrence recurrence, DateOnly? endDate, int? count)
    {
        if (recurrence != Recurrence.None && endDate is null && count is null)
            return "Recurring transactions must have either an end date or an occurrence count.";

        if (recurrence == Recurrence.None && (endDate is not null || count is not null))
            return "Non-recurring transactions cannot have an end date or occurrence count.";

        if (count is not null && count <= 0)
            return "Occurrence count must be greater than zero.";

        return null;
    }

    private static DateOnly AdvanceDate(DateOnly date, Recurrence recurrence)
    {
        return recurrence switch
        {
            Recurrence.Daily => date.AddDays(1),
            Recurrence.Weekly => date.AddDays(7),
            Recurrence.Monthly => date.AddMonths(1),
            Recurrence.Yearly => date.AddYears(1),
            _ => date
        };
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
