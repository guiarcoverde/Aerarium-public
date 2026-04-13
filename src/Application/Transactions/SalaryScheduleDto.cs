namespace Aerarium.Application.Transactions;

using Aerarium.Domain.Enums;

public sealed record SalaryScheduleDto(
    SalaryScheduleMode Mode,
    int? BusinessDayNumber,
    int? FixedDay,
    decimal? SplitFirstAmount,
    decimal? SplitFirstPercentage);
