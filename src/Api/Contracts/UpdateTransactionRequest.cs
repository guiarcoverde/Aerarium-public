namespace Aerarium.Api.Contracts;

using Aerarium.Domain.Enums;

public sealed record UpdateTransactionRequest(
    decimal Amount,
    string Description,
    DateOnly Date,
    TransactionType Type,
    TransactionCategory Category,
    Recurrence Recurrence = Recurrence.None,
    DateOnly? RecurrenceEndDate = null,
    int? RecurrenceCount = null,
    SalaryScheduleRequest? SalarySchedule = null,
    PaymentMethod? PaymentMethod = null,
    Guid? BankAccountId = null,
    Guid? CardId = null);
