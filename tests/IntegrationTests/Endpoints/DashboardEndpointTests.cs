namespace Aerarium.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Aerarium.Application.Dashboard;
using Aerarium.Domain.Enums;
using Aerarium.IntegrationTests.Infrastructure;
using FluentAssertions;

public sealed class DashboardEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetSummary_MixedTransactions_ReturnsCorrectBalance()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "dash-user", email: "dash@test.com");

        await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 1000m, Description = "Salary", Date = "2026-04-01",
            Type = TransactionType.Income, Category = TransactionCategory.Salary
        });

        await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 300m, Description = "Groceries", Date = "2026-04-05",
            Type = TransactionType.Expense, Category = TransactionCategory.Grocery
        });

        var response = await client.GetAsync("/api/dashboard/summary?month=4&year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary.Should().NotBeNull();
        summary!.TotalIncome.Should().Be(1000m);
        summary.TotalExpenses.Should().Be(300m);
        summary.Balance.Should().Be(700m);
    }

    [Fact]
    public async Task GetSummary_NoTransactions_ReturnsZeros()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "empty-dash-user", email: "emptydash@test.com");

        var response = await client.GetAsync("/api/dashboard/summary?month=1&year=2020");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary.Should().NotBeNull();
        summary!.TotalIncome.Should().Be(0);
        summary.TotalExpenses.Should().Be(0);
        summary.Balance.Should().Be(0);
    }
}
