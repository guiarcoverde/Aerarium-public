namespace Aerarium.Api.Endpoints;

using Aerarium.Application.Cards.Create;
using Aerarium.Application.Cards.Delete;
using Aerarium.Application.Cards.GetById;
using Aerarium.Application.Cards.List;
using Aerarium.Application.Cards.Update;
using Aerarium.Domain.Enums;
using Mediator;

public static class CardsEndpoints
{
    public sealed record CreateCardRequest(string Name, decimal CreditLimit, CardType Type, Guid? LinkedBankAccountId);
    public sealed record UpdateCardRequest(string Name, CardType Type, decimal CreditLimit, Guid? LinkedBankAccountId);

    public static void MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cards")
            .WithTags("Cards")
            .RequireAuthorization();

        group.MapPost("/", async (CreateCardRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateCardCommand(
                request.Name, request.CreditLimit, request.Type, request.LinkedBankAccountId));

            return result.IsSuccess
                ? Results.Created($"/api/cards/{result.Value!.Id}", result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCardsQuery());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCardQuery(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCardRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateCardCommand(
                id, request.Name, request.Type, request.CreditLimit, request.LinkedBankAccountId));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error,
                    statusCode: result.Error == "Card not found."
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteCardCommand(id));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(title: result.Error,
                    statusCode: result.Error == "Card not found."
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest);
        });
    }
}
