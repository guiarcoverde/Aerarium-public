namespace Aerarium.Api.Endpoints;

using Aerarium.Application.BankAccounts.Create;
using Aerarium.Application.BankAccounts.Delete;
using Aerarium.Application.BankAccounts.GetById;
using Aerarium.Application.BankAccounts.List;
using Aerarium.Application.BankAccounts.Update;
using Mediator;

public static class BankAccountsEndpoints
{
    public sealed record CreateBankAccountRequest(string Name, decimal Balance);
    public sealed record UpdateBankAccountRequest(string Name);

    public static void MapBankAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bank-accounts")
            .WithTags("Bank Accounts")
            .RequireAuthorization();

        group.MapPost("/", async (CreateBankAccountRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateBankAccountCommand(request.Name, request.Balance));

            return result.IsSuccess
                ? Results.Created($"/api/bank-accounts/{result.Value!.Id}", result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListBankAccountsQuery());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBankAccountQuery(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateBankAccountRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateBankAccountCommand(id, request.Name));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error,
                    statusCode: result.Error == "Bank account not found."
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteBankAccountCommand(id));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(title: result.Error,
                    statusCode: result.Error == "Bank account not found."
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest);
        });
    }
}
