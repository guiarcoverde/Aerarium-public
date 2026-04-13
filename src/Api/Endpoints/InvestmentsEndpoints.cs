namespace Aerarium.Api.Endpoints;

using Aerarium.Application.Investments.Create;
using Aerarium.Application.Investments.Delete;
using Aerarium.Application.Investments.GetById;
using Aerarium.Application.Investments.List;
using Aerarium.Application.Investments.Update;
using Aerarium.Domain.Enums;
using Mediator;

public static class InvestmentsEndpoints
{
    public sealed record CreateInvestmentRequest(
        string Name,
        decimal Amount,
        decimal CurrentValue,
        InvestmentType Type,
        DateOnly PurchaseDate);

    public sealed record UpdateInvestmentRequest(
        string Name,
        decimal Amount,
        decimal CurrentValue,
        InvestmentType Type,
        DateOnly PurchaseDate);

    public static void MapInvestmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investments")
            .WithTags("Investments")
            .RequireAuthorization();

        group.MapPost("/", async (CreateInvestmentRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateInvestmentCommand(
                request.Name, request.Amount, request.CurrentValue, request.Type, request.PurchaseDate));

            return result.IsSuccess
                ? Results.Created($"/api/investments/{result.Value!.Id}", result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/", async (int page, int pageSize, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListInvestmentsQuery(
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 10 : pageSize));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetInvestmentQuery(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateInvestmentRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateInvestmentCommand(
                id, request.Name, request.Amount, request.CurrentValue, request.Type, request.PurchaseDate));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error,
                    statusCode: result.Error == "Investment not found."
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteInvestmentCommand(id));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });
    }
}
