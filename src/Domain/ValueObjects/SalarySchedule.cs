namespace Aerarium.Domain.ValueObjects;

using Aerarium.Domain.Common;
using Aerarium.Domain.Enums;

public sealed class SalarySchedule
{
    public SalaryScheduleMode Mode { get; private set; }
    public int? BusinessDayNumber { get; private set; }
    public int? FixedDay { get; private set; }
    public decimal? SplitFirstAmount { get; private set; }
    public decimal? SplitFirstPercentage { get; private set; }

    private SalarySchedule() { }

    public static Result<SalarySchedule> Create(
        SalaryScheduleMode mode,
        int? businessDayNumber = null,
        int? fixedDay = null,
        decimal? splitFirstAmount = null,
        decimal? splitFirstPercentage = null)
    {
        switch (mode)
        {
            case SalaryScheduleMode.BusinessDay:
                if (businessDayNumber is null or < 1 or > 23)
                    return Result<SalarySchedule>.Failure("Business day number must be between 1 and 23.");
                if (fixedDay is not null || splitFirstAmount is not null || splitFirstPercentage is not null)
                    return Result<SalarySchedule>.Failure("Business day mode does not support fixed day or split configuration.");
                break;

            case SalaryScheduleMode.FixedDate:
                if (fixedDay is null or < 1 or > 31)
                    return Result<SalarySchedule>.Failure("Fixed day must be between 1 and 31.");
                if (businessDayNumber is not null || splitFirstAmount is not null || splitFirstPercentage is not null)
                    return Result<SalarySchedule>.Failure("Fixed date mode does not support business day number or split configuration.");
                break;

            case SalaryScheduleMode.FixedDateSplit:
                if (fixedDay is null or < 1 or > 31)
                    return Result<SalarySchedule>.Failure("Fixed day must be between 1 and 31.");
                if (businessDayNumber is not null)
                    return Result<SalarySchedule>.Failure("Fixed date split mode does not support business day number.");
                if (splitFirstAmount is null && splitFirstPercentage is null)
                    return Result<SalarySchedule>.Failure("Split mode requires either a first part amount or a first part percentage.");
                if (splitFirstAmount is not null && splitFirstPercentage is not null)
                    return Result<SalarySchedule>.Failure("Split mode requires either amount or percentage, not both.");
                if (splitFirstAmount is not null && splitFirstAmount <= 0)
                    return Result<SalarySchedule>.Failure("Split first amount must be greater than zero.");
                if (splitFirstPercentage is not null && splitFirstPercentage is <= 0 or > 100)
                    return Result<SalarySchedule>.Failure("Split first percentage must be between 0 (exclusive) and 100.");
                break;

            case SalaryScheduleMode.None:
                if (businessDayNumber is not null || fixedDay is not null || splitFirstAmount is not null || splitFirstPercentage is not null)
                    return Result<SalarySchedule>.Failure("None mode does not accept any schedule configuration.");
                break;

            default:
                return Result<SalarySchedule>.Failure("Invalid salary schedule mode.");
        }

        return new SalarySchedule
        {
            Mode = mode,
            BusinessDayNumber = businessDayNumber,
            FixedDay = fixedDay,
            SplitFirstAmount = splitFirstAmount,
            SplitFirstPercentage = splitFirstPercentage
        };
    }
}
