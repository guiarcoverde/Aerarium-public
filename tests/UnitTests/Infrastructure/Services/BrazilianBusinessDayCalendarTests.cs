namespace Aerarium.UnitTests.Infrastructure.Services;

using Aerarium.Infrastructure.Services;
using FluentAssertions;

public sealed class BrazilianBusinessDayCalendarTests
{
    private readonly BrazilianBusinessDayCalendar _calendar = new();

    [Fact]
    public void GetNthBusinessDay_FifthBusinessDay_April2026()
    {
        // April 2026: 1=Wed, 2=Thu, 3=Fri, 6=Mon, 7=Tue (5th BD)
        var result = _calendar.GetNthBusinessDay(2026, 4, 5);
        result.Should().Be(new DateOnly(2026, 4, 7));
    }

    [Fact]
    public void GetPreviousBusinessDay_Weekend_ReturnsFriday()
    {
        // April 4, 2026 is Saturday
        var result = _calendar.GetPreviousBusinessDay(new DateOnly(2026, 4, 4));
        result.Should().Be(new DateOnly(2026, 4, 3)); // Friday
    }

    [Fact]
    public void GetPreviousBusinessDay_BusinessDay_ReturnsSameDay()
    {
        // April 3, 2026 is Friday
        var result = _calendar.GetPreviousBusinessDay(new DateOnly(2026, 4, 3));
        result.Should().Be(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void GetLastBusinessDay_ReturnsCorrect()
    {
        // April 2026: 30th is Thursday (business day)
        var result = _calendar.GetLastBusinessDay(2026, 4);
        result.Should().Be(new DateOnly(2026, 4, 30));
    }

    [Fact]
    public void IsBusinessDay_Holiday_ReturnsFalse()
    {
        // April 21, 2026 is Tiradentes (Tuesday)
        _calendar.IsBusinessDay(new DateOnly(2026, 4, 21)).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessDay_Weekend_ReturnsFalse()
    {
        _calendar.IsBusinessDay(new DateOnly(2026, 4, 4)).Should().BeFalse(); // Saturday
    }

    [Fact]
    public void IsBusinessDay_RegularWeekday_ReturnsTrue()
    {
        _calendar.IsBusinessDay(new DateOnly(2026, 4, 1)).Should().BeTrue(); // Wednesday
    }

    [Fact]
    public void GetNthBusinessDay_AccountsForHoliday()
    {
        // April 2026: Tiradentes on Apr 21 (Tue)
        // BDs from Apr 20: 20=Mon(BD14), 21=Holiday, 22=Wed(BD15)
        var result = _calendar.GetNthBusinessDay(2026, 4, 15);
        result.Should().Be(new DateOnly(2026, 4, 22));
    }
}
