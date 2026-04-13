namespace Aerarium.UnitTests.Domain.Entities;

using Aerarium.Domain.Entities;
using Aerarium.Domain.Enums;
using FluentAssertions;

public sealed class CardTests
{
    private const string ValidUserId = "user-123";
    private const string ValidName = "Visa Gold";
    private const decimal ValidLimit = 5_000m;
    private static readonly Guid ValidBankAccountId = Guid.CreateVersion7();

    [Fact]
    public void Create_CreditOnly_ReturnsSuccessWithFullAvailableLimit()
    {
        var result = Card.Create(ValidUserId, ValidName, ValidLimit, CardType.Credit, null);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value!;
        card.Name.Should().Be(ValidName);
        card.Type.Should().Be(CardType.Credit);
        card.CreditLimit.Should().Be(ValidLimit);
        card.AvailableLimit.Should().Be(ValidLimit);
        card.LinkedBankAccountId.Should().BeNull();
        card.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_DebitOnly_ZeroesCreditFieldsAndKeepsLink()
    {
        var result = Card.Create(ValidUserId, ValidName, 999m, CardType.Debit, ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value!;
        card.Type.Should().Be(CardType.Debit);
        card.CreditLimit.Should().Be(0m);
        card.AvailableLimit.Should().Be(0m);
        card.LinkedBankAccountId.Should().Be(ValidBankAccountId);
    }

    [Fact]
    public void Create_BothFlags_RequiresLinkedBankAndPositiveLimit()
    {
        var result = Card.Create(ValidUserId, ValidName, ValidLimit, CardType.Debit | CardType.Credit, ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value!;
        card.Type.Should().Be(CardType.Debit | CardType.Credit);
        card.CreditLimit.Should().Be(ValidLimit);
        card.AvailableLimit.Should().Be(ValidLimit);
        card.LinkedBankAccountId.Should().Be(ValidBankAccountId);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var result = Card.Create(ValidUserId, "  Mastercard  ", ValidLimit, CardType.Credit, null);

        result.Value!.Name.Should().Be("Mastercard");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ReturnsFailure(string? name)
    {
        var result = Card.Create(ValidUserId, name!, ValidLimit, CardType.Credit, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Name is required");
    }

    [Fact]
    public void Create_WithNoTypeFlags_ReturnsFailure()
    {
        var result = Card.Create(ValidUserId, ValidName, ValidLimit, default, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("debit, credit, or both");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_CreditCardWithNonPositiveLimit_ReturnsFailure(decimal limit)
    {
        var result = Card.Create(ValidUserId, ValidName, limit, CardType.Credit, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Credit limit");
    }

    [Fact]
    public void Create_DebitCardWithoutLinkedBank_ReturnsFailure()
    {
        var result = Card.Create(ValidUserId, ValidName, 0m, CardType.Debit, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("bank account is required");
    }

    [Fact]
    public void ConsumeLimit_OnCreditCard_ReducesAvailable()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;

        var result = card.ConsumeLimit(300m);

        result.IsSuccess.Should().BeTrue();
        card.AvailableLimit.Should().Be(700m);
        card.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ConsumeLimit_OnDebitOnlyCard_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 0m, CardType.Debit, ValidBankAccountId).Value!;

        var result = card.ConsumeLimit(50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("does not support credit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConsumeLimit_WithNonPositiveAmount_ReturnsFailure(decimal amount)
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;

        var result = card.ConsumeLimit(amount);

        result.IsFailure.Should().BeTrue();
        card.AvailableLimit.Should().Be(1_000m);
    }

    [Fact]
    public void ConsumeLimit_BeyondAvailable_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;

        var result = card.ConsumeLimit(1_500m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient");
        card.AvailableLimit.Should().Be(1_000m);
    }

    [Fact]
    public void RestoreLimit_AfterConsume_RestoresAvailable()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(400m);

        var result = card.RestoreLimit(150m);

        result.IsSuccess.Should().BeTrue();
        card.AvailableLimit.Should().Be(750m);
    }

    [Fact]
    public void RestoreLimit_BeyondCreditLimit_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(200m);

        var result = card.RestoreLimit(500m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceed credit limit");
        card.AvailableLimit.Should().Be(800m);
    }

    [Fact]
    public void RestoreLimit_OnDebitOnlyCard_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 0m, CardType.Debit, ValidBankAccountId).Value!;

        var result = card.RestoreLimit(50m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Update_RaisingCreditLimit_ReconcilesAvailable()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(200m); // available = 800

        var result = card.Update(ValidName, CardType.Credit, 1_500m, null);

        result.IsSuccess.Should().BeTrue();
        card.CreditLimit.Should().Be(1_500m);
        card.AvailableLimit.Should().Be(1_300m); // 1500 - 200 consumed
    }

    [Fact]
    public void Update_LoweringCreditLimit_ReconcilesAvailable()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(400m); // available = 600

        var result = card.Update(ValidName, CardType.Credit, 500m, null);

        result.IsSuccess.Should().BeTrue();
        card.CreditLimit.Should().Be(500m);
        card.AvailableLimit.Should().Be(100m); // 500 - 400 consumed
    }

    [Fact]
    public void Update_NewLimitBelowConsumed_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(800m); // available = 200

        var result = card.Update(ValidName, CardType.Credit, 500m, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already consumed");
        card.CreditLimit.Should().Be(1_000m);
        card.AvailableLimit.Should().Be(200m);
    }

    [Fact]
    public void Update_LosingCreditFlag_ZeroesCreditFields()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Debit | CardType.Credit, ValidBankAccountId).Value!;

        var result = card.Update(ValidName, CardType.Debit, 0m, ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        card.CreditLimit.Should().Be(0m);
        card.AvailableLimit.Should().Be(0m);
        card.LinkedBankAccountId.Should().Be(ValidBankAccountId);
    }

    [Fact]
    public void Update_LosingDebitFlag_ClearsLinkedBankAccount()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Debit | CardType.Credit, ValidBankAccountId).Value!;

        var result = card.Update(ValidName, CardType.Credit, 1_000m, ValidBankAccountId);

        result.IsSuccess.Should().BeTrue();
        card.LinkedBankAccountId.Should().BeNull();
    }

    [Fact]
    public void Update_TrimsName()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;

        card.Update("  Elo  ", CardType.Credit, 1_000m, null);

        card.Name.Should().Be("Elo");
    }

    [Fact]
    public void EnsureCanBeDeleted_WithNoConsumedLimit_ReturnsSuccess()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;

        var result = card.EnsureCanBeDeleted();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureCanBeDeleted_WithConsumedLimit_ReturnsFailure()
    {
        var card = Card.Create(ValidUserId, ValidName, 1_000m, CardType.Credit, null).Value!;
        card.ConsumeLimit(100m);

        var result = card.EnsureCanBeDeleted();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("consumed credit limit");
    }

    [Fact]
    public void EnsureCanBeDeleted_DebitOnlyCard_ReturnsSuccess()
    {
        var card = Card.Create(ValidUserId, ValidName, 0m, CardType.Debit, ValidBankAccountId).Value!;

        var result = card.EnsureCanBeDeleted();

        result.IsSuccess.Should().BeTrue();
    }
}
