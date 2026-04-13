namespace Aerarium.UnitTests.Application.Dashboard;

using Aerarium.Application.Common;
using Aerarium.Application.Dashboard;
using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;

public sealed class GetDashboardSummaryHandlerTests
{
    private readonly IAppDbContext _dbContext = Substitute.For<IAppDbContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICategoryLocalizer _categoryLocalizer = Substitute.For<ICategoryLocalizer>();
    private readonly GetDashboardSummaryHandler _handler;

    public GetDashboardSummaryHandlerTests()
    {
        _currentUser.UserId.Returns("user-123");
        _categoryLocalizer.GetDisplayName(Arg.Any<TransactionCategory>()).Returns(c => c.Arg<TransactionCategory>().ToString());

        var investmentsMock = new List<Investment>().BuildMockDbSet();
        _dbContext.Investments.Returns(investmentsMock);

        _handler = new GetDashboardSummaryHandler(_dbContext, _currentUser, _categoryLocalizer);
    }

    [Fact]
    public async Task Handle_WithTransactions_ReturnsCorrectTotals()
    {
        var transactions = new List<Transaction>
        {
            CreateTransaction("user-123", 1000m, TransactionType.Income, TransactionCategory.Salary, new DateOnly(2026, 4, 5)),
            CreateTransaction("user-123", 500m, TransactionType.Income, TransactionCategory.Bonus, new DateOnly(2026, 4, 10)),
            CreateTransaction("user-123", 200m, TransactionType.Expense, TransactionCategory.Grocery, new DateOnly(2026, 4, 3)),
            CreateTransaction("user-123", 300m, TransactionType.Expense, TransactionCategory.Housing, new DateOnly(2026, 4, 1)),
        };

        var mockDbSet = transactions.BuildMockDbSet();
        _dbContext.Transactions.Returns(mockDbSet);

        var query = new GetDashboardSummaryQuery(4, 2026);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalIncome.Should().Be(1500m);
        result.Value.TotalExpenses.Should().Be(500m);
        result.Value.Balance.Should().Be(1000m);
        result.Value.IncomeByCategory.Should().HaveCount(2);
        result.Value.ExpenseByCategory.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsZeros()
    {
        var mockDbSet = new List<Transaction>().BuildMockDbSet();
        _dbContext.Transactions.Returns(mockDbSet);

        var query = new GetDashboardSummaryQuery(4, 2026);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalIncome.Should().Be(0);
        result.Value.TotalExpenses.Should().Be(0);
        result.Value.Balance.Should().Be(0);
        result.Value.IncomeByCategory.Should().BeEmpty();
        result.Value.ExpenseByCategory.Should().BeEmpty();
    }

    private static readonly Guid TestBankAccountId = Guid.CreateVersion7();
    private static readonly Guid TestCardId = Guid.CreateVersion7();

    private static Transaction CreateTransaction(
        string userId, decimal amount, TransactionType type,
        TransactionCategory category, DateOnly date)
    {
        return Transaction.Create(userId, amount, "Test", date, type, category,
            paymentMethod: type == TransactionType.Expense ? PaymentMethod.Pix : null,
            bankAccountId: type == TransactionType.Income || type == TransactionType.Expense ? TestBankAccountId : null).Value!;
    }
}
