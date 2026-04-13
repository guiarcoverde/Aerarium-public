namespace Aerarium.Domain.Common;

public interface IBusinessDayCalendar
{
    DateOnly GetNthBusinessDay(int year, int month, int n);
    DateOnly GetPreviousBusinessDay(DateOnly date);
    DateOnly GetLastBusinessDay(int year, int month);
    bool IsBusinessDay(DateOnly date);
}
