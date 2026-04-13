namespace Aerarium.UnitTests.Application.Transactions;

using Aerarium.Application.Common;
using Aerarium.Application.Transactions.Create;
using Aerarium.Domain.Common;
using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;

public sealed class CreateTransactionHandlerTests
{
    private readonly IAppDbContext _dbContext = Substitute.For<IAppDbContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICategoryLocalizer _categoryLocalizer = Substitute.For<ICategoryLocalizer>();
    private readonly IPaymentMethodLocalizer _paymentMethodLocalizer = Substitute.For<IPaymentMethodLocalizer>();
    private readonly IBusinessDayCalendar _calendar = Substitute.For<IBusinessDayCalendar>();
    private readonly CreateTransactionHandler _handler;
    private readonly Guid _bankAccountId;

    public CreateTransactionHandlerTests()
    {
        _currentUser.UserId.Returns("user-123");
        _categoryLocalizer.GetDisplayName(Arg.Any<TransactionCategory>()).Returns(c => c.Arg<TransactionCategory>().ToString());
        _paymentMethodLocalizer.GetDisplayName(Arg.Any<PaymentMethod>()).Returns(c => c.Arg<PaymentMethod>().ToString());

        var bank = BankAccount.Create("user-123", "Nubank", 5000m).Value!;
        _bankAccountId = bank.Id;

        var transactions = new List<Transaction>().BuildMockDbSet();
        _dbContext.Transactions.Returns(transactions);

        var bankAccounts = new List<BankAccount> { bank }.BuildMockDbSet();
        _dbContext.BankAccounts.Returns(bankAccounts);

        var cards = new List<Card>().BuildMockDbSet();
        _dbContext.Cards.Returns(cards);

        _handler = new CreateTransactionHandler(_dbContext, _currentUser, _categoryLocalizer, _paymentMethodLocalizer, _calendar);
    }

    [Fact]
    public async Task Handle_ValidIncomeCommand_CreatesAndReturnsDto()
    {
        var command = new CreateTransactionCommand(
            150.50m, "Salary", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Salary,
            BankAccountId: _bankAccountId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(150.50m);
        result.Value.Description.Should().Be("Salary");
        result.Value.Type.Should().Be(TransactionType.Income);
        result.Value.Category.Should().Be(TransactionCategory.Salary);
        result.Value.BankAccountId.Should().Be(_bankAccountId);

        _dbContext.Transactions.Received(1).Add(Arg.Any<Transaction>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCategory_ReturnsFailure()
    {
        var command = new CreateTransactionCommand(
            100m, "Test", new DateOnly(2026, 4, 1),
            TransactionType.Income, TransactionCategory.Housing,
            BankAccountId: _bankAccountId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Category");
    }
}
