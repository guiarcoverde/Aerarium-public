namespace Aerarium.Application.Transactions.Update;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;
using Mediator;

public sealed record UpdateTransactionCommand(
    Guid Id,
    decimal Amount,
    string Description,
    DateOnly Date,
    TransactionType Type,
    TransactionCategory Category,
    Recurrence Recurrence = Recurrence.None,
    DateOnly? RecurrenceEndDate = null,
    int? RecurrenceCount = null,
    SalarySchedule? SalarySchedule = null,
    PaymentMethod? PaymentMethod = null,
    Guid? BankAccountId = null,
    Guid? CardId = null) : ICommand<Result<TransactionDto>>;
