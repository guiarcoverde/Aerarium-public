namespace Aerarium.Api.Endpoints;

using Aerarium.Application.Common;
using Aerarium.Domain.Enums;

public static class CategoriesEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", (TransactionType? type, ICategoryLocalizer categoryLocalizer) =>
        {
            var categories = Enum.GetValues<TransactionCategory>()
                .Where(c => type switch
                {
                    TransactionType.Income => (int)c is >= 100 and < 200,
                    TransactionType.Expense => (int)c is >= 200 and < 300,
                    _ => true
                })
                .Select(c => new
                {
                    Category = c,
                    DisplayName = categoryLocalizer.GetDisplayName(c)
                })
                .ToList();

            return Results.Ok(categories);
        });
    }
}
