namespace Aerarium.Infrastructure.Services;

using Aerarium.Domain.Common;

public sealed class BrazilianBusinessDayCalendar : IBusinessDayCalendar
{
    public bool IsBusinessDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        return !IsHoliday(date);
    }

    public DateOnly GetNthBusinessDay(int year, int month, int n)
    {
        var date = new DateOnly(year, month, 1);
        var count = 0;

        while (true)
        {
            if (IsBusinessDay(date))
            {
                count++;
                if (count == n)
                    return date;
            }

            date = date.AddDays(1);

            if (date.Month != month)
                return GetLastBusinessDay(year, month);
        }
    }

    public DateOnly GetPreviousBusinessDay(DateOnly date)
    {
        var current = date;

        while (!IsBusinessDay(current))
            current = current.AddDays(-1);

        return current;
    }

    public DateOnly GetLastBusinessDay(int year, int month)
    {
        var lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return GetPreviousBusinessDay(lastDay);
    }

    private static bool IsHoliday(DateOnly date)
    {
        var holidays = GetHolidays(date.Year);
        return holidays.Contains(date);
    }

    private static HashSet<DateOnly> GetHolidays(int year)
    {
        var holidays = new HashSet<DateOnly>
        {
            new(year, 1, 1),   // New Year
            new(year, 4, 21),  // Tiradentes
            new(year, 5, 1),   // Labor Day
            new(year, 9, 7),   // Independence Day
            new(year, 10, 12), // Nossa Senhora Aparecida
            new(year, 11, 2),  // Finados
            new(year, 11, 15), // Republic Day
            new(year, 12, 25), // Christmas
        };

        var easter = CalculateEaster(year);
        holidays.Add(easter.AddDays(-47)); // Carnival Monday
        holidays.Add(easter.AddDays(-46)); // Carnival Tuesday
        holidays.Add(easter.AddDays(-2));  // Good Friday
        holidays.Add(easter.AddDays(60));  // Corpus Christi

        return holidays;
    }

    private static DateOnly CalculateEaster(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;

        return new DateOnly(year, month, day);
    }
}
