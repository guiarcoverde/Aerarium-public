namespace Aerarium.Application.Transactions;

using Aerarium.Domain.Enums;

public sealed record TransactionDto(
    Guid Id,
    decimal Amount,
    string Description,
    DateOnly Date,
    TransactionType Type,
    TransactionCategory Category,
    string CategoryDisplayName,
    PaymentMethod? PaymentMethod,
    string? PaymentMethodDisplayName,
    Recurrence Recurrence,
    Guid? RecurrenceGroupId,
    DateOnly? RecurrenceEndDate,
    int? RecurrenceCount,
    SalaryScheduleDto? SalarySchedule,
    Guid? BankAccountId,
    string? BankAccountName,
    Guid? CardId,
    string? CardName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
