namespace Aerarium.Api.Endpoints;

using Aerarium.Application.Dashboard;
using Mediator;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", async (int month, int year, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDashboardSummaryQuery(month, year));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
