namespace Aerarium.Application.Dashboard;

using Aerarium.Domain.Enums;

public sealed record DashboardSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    decimal TotalInvested,
    IReadOnlyList<CategoryBreakdownDto> IncomeByCategory,
    IReadOnlyList<CategoryBreakdownDto> ExpenseByCategory);

public sealed record CategoryBreakdownDto(
    TransactionCategory Category,
    string CategoryDisplayName,
    decimal Total,
    int Count);
