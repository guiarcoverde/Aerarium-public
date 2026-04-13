namespace Aerarium.Application.BankAccounts.GetById;

using Aerarium.Domain.Common;
using Mediator;

public sealed record GetBankAccountQuery(Guid Id) : IQuery<Result<BankAccountDto>>;
