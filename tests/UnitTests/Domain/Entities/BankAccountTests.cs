namespace Aerarium.UnitTests.Domain.Entities;

using Aerarium.Domain.Entities;
using FluentAssertions;

public sealed class BankAccountTests
{
    private const string ValidUserId = "user-123";
    private const string ValidName = "Nubank";
    private const decimal InitialBalance = 1_000m;

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var result = BankAccount.Create(ValidUserId, ValidName, InitialBalance);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(ValidUserId);
        result.Value.Name.Should().Be(ValidName);
        result.Value.Balance.Should().Be(InitialBalance);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var result = BankAccount.Create(ValidUserId, "  Itaú  ", InitialBalance);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Itaú");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ReturnsFailure(string? name)
    {
        var result = BankAccount.Create(ValidUserId, name!, InitialBalance);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Fact]
    public void Create_AllowsNegativeInitialBalance()
    {
        var result = BankAccount.Create(ValidUserId, ValidName, -50m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Balance.Should().Be(-50m);
    }

    [Fact]
    public void Credit_WithPositiveAmount_IncreasesBalance()
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Credit(50m);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(150m);
        account.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Credit_WithNonPositiveAmount_ReturnsFailure(decimal amount)
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Credit(amount);

        result.IsFailure.Should().BeTrue();
        account.Balance.Should().Be(100m);
        account.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Debit_WithPositiveAmount_DecreasesBalance()
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Debit(30m);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(70m);
        account.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Debit_AllowsNegativeBalance()
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 50m).Value!;

        var result = account.Debit(200m);

        result.IsSuccess.Should().BeTrue();
        account.Balance.Should().Be(-150m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Debit_WithNonPositiveAmount_ReturnsFailure(decimal amount)
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Debit(amount);

        result.IsFailure.Should().BeTrue();
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Rename("  Inter  ");

        result.IsSuccess.Should().BeTrue();
        account.Name.Should().Be("Inter");
        account.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ReturnsFailure(string name)
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.Rename(name);

        result.IsFailure.Should().BeTrue();
        account.Name.Should().Be(ValidName);
    }

    [Fact]
    public void EnsureCanBeDeleted_ReturnsSuccess()
    {
        var account = BankAccount.Create(ValidUserId, ValidName, 100m).Value!;

        var result = account.EnsureCanBeDeleted();

        result.IsSuccess.Should().BeTrue();
    }
}
