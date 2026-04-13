namespace Aerarium.Api.Contracts;

using Aerarium.Domain.Enums;

public sealed record SalaryScheduleRequest(
    SalaryScheduleMode Mode,
    int? BusinessDayNumber = null,
    int? FixedDay = null,
    decimal? SplitFirstAmount = null,
    decimal? SplitFirstPercentage = null);
