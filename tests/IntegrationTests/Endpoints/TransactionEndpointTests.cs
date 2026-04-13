namespace Aerarium.IntegrationTests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using Aerarium.Application.Common;
using Aerarium.Application.Transactions;
using Aerarium.Domain.Enums;
using Aerarium.IntegrationTests.Infrastructure;
using FluentAssertions;

public sealed class TransactionEndpointTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateTransaction_ValidRequest_Returns201()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var request = new
        {
            Amount = 100.50m,
            Description = "Test income",
            Date = "2026-04-01",
            Type = TransactionType.Income,
            Category = TransactionCategory.Salary
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDto>();
        dto.Should().NotBeNull();
        dto!.Amount.Should().Be(100.50m);
        dto.Description.Should().Be("Test income");
    }

    [Fact]
    public async Task CreateTransaction_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var request = new
        {
            Amount = 100m,
            Description = "Test",
            Date = "2026-04-01",
            Type = TransactionType.Income,
            Category = TransactionCategory.Salary
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransaction_OtherUsersTransaction_Returns404()
    {
        var client1 = await factory.CreateAuthenticatedClientAsync(userId: "user-1", email: "user1@test.com");
        var createRequest = new
        {
            Amount = 50m,
            Description = "User1 transaction",
            Date = "2026-04-01",
            Type = TransactionType.Expense,
            Category = TransactionCategory.Grocery
        };

        var createResponse = await client1.PostAsJsonAsync("/api/transactions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var client2 = await factory.CreateAuthenticatedClientAsync(userId: "user-2", email: "user2@test.com");
        var response = await client2.GetAsync($"/api/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListTransactions_FilterByMonth_ReturnsFiltered()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "list-user", email: "list@test.com");

        await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 100m, Description = "April", Date = "2026-04-15",
            Type = TransactionType.Income, Category = TransactionCategory.Salary
        });

        await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 200m, Description = "March", Date = "2026-03-15",
            Type = TransactionType.Income, Category = TransactionCategory.Bonus
        });

        var response = await client.GetAsync("/api/transactions?month=4&year=2026&page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().AllSatisfy(t => t.Date.Month.Should().Be(4));
    }

    [Fact]
    public async Task DeleteTransaction_Existing_Returns204()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "delete-user", email: "delete@test.com");
        var createResponse = await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 50m, Description = "To delete", Date = "2026-04-01",
            Type = TransactionType.Expense, Category = TransactionCategory.Housing
        });

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();

        var response = await client.DeleteAsync($"/api/transactions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateTransaction_Recurring_CreatesMultipleLinked()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "recur-user", email: "recur@test.com");
        var request = new
        {
            Amount = 500m,
            Description = "Monthly rent",
            Date = "2026-04-01",
            Type = TransactionType.Expense,
            Category = TransactionCategory.Housing,
            Recurrence = Recurrence.Monthly,
            RecurrenceCount = 3
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDto>();
        dto!.Recurrence.Should().Be(Recurrence.Monthly);
        dto.RecurrenceGroupId.Should().NotBeNull();

        var listResponse = await client.GetAsync("/api/transactions?page=1&pageSize=50");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        var seriesItems = result!.Items.Where(t => t.RecurrenceGroupId == dto.RecurrenceGroupId).ToList();
        seriesItems.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteSeries_RemovesAllOccurrences()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "del-series-user", email: "delseries@test.com");
        var createResponse = await client.PostAsJsonAsync("/api/transactions", new
        {
            Amount = 100m,
            Description = "Weekly gym",
            Date = "2026-04-01",
            Type = TransactionType.Expense,
            Category = TransactionCategory.Health,
            Recurrence = Recurrence.Weekly,
            RecurrenceCount = 4
        });

        var created = await createResponse.Content.ReadFromJsonAsync<TransactionDto>();
        var groupId = created!.RecurrenceGroupId;

        var deleteResponse = await client.DeleteAsync($"/api/transactions/series/{groupId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/transactions?page=1&pageSize=50");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        result!.Items.Where(t => t.RecurrenceGroupId == groupId).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_SalaryWithBusinessDaySchedule_GeneratesCorrectDates()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "sal-bd-user", email: "salbd@test.com");
        var request = new
        {
            Amount = 5000m,
            Description = "Monthly salary",
            Date = "2026-04-01",
            Type = TransactionType.Income,
            Category = TransactionCategory.Salary,
            Recurrence = Recurrence.Monthly,
            RecurrenceCount = 3,
            SalarySchedule = new
            {
                Mode = SalaryScheduleMode.BusinessDay,
                BusinessDayNumber = 5
            }
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDto>();
        dto!.SalarySchedule.Should().NotBeNull();
        dto.SalarySchedule!.Mode.Should().Be(SalaryScheduleMode.BusinessDay);

        var listResponse = await client.GetAsync("/api/transactions?page=1&pageSize=50");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        var seriesItems = result!.Items.Where(t => t.RecurrenceGroupId == dto.RecurrenceGroupId).ToList();
        seriesItems.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateTransaction_SalarySplit_GeneratesDoubleTransactions()
    {
        var client = await factory.CreateAuthenticatedClientAsync(userId: "sal-split-user", email: "salsplit@test.com");
        var request = new
        {
            Amount = 10000m,
            Description = "Split salary",
            Date = "2026-04-01",
            Type = TransactionType.Income,
            Category = TransactionCategory.Salary,
            Recurrence = Recurrence.Monthly,
            RecurrenceCount = 2,
            SalarySchedule = new
            {
                Mode = SalaryScheduleMode.FixedDateSplit,
                FixedDay = 15,
                SplitFirstPercentage = 40m
            }
        };

        var response = await client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/transactions?page=1&pageSize=50");
        var result = await listResponse.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>();
        var dto = (await response.Content.ReadFromJsonAsync<TransactionDto>())!;
        var seriesItems = result!.Items.Where(t => t.RecurrenceGroupId == dto.RecurrenceGroupId).ToList();
        // 2 months × 2 payments = 4 transactions
        seriesItems.Should().HaveCount(4);
        seriesItems.Should().Contain(t => t.Amount == 4000m);
        seriesItems.Should().Contain(t => t.Amount == 6000m);
    }
}
