namespace Aerarium.Api.Endpoints;

using Aerarium.Api.Contracts;
using Aerarium.Application.Transactions;
using Aerarium.Application.Transactions.Create;
using Aerarium.Application.Transactions.Delete;
using Aerarium.Application.Transactions.GetById;
using Aerarium.Application.Transactions.List;
using Aerarium.Application.Transactions.Update;
using Aerarium.Domain.Enums;
using Aerarium.Domain.ValueObjects;
using Mediator;

public static class TransactionsEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapPost("/", async (CreateTransactionRequest request, IMediator mediator) =>
        {
            var salarySchedule = MapSalarySchedule(request.SalarySchedule);
            if (salarySchedule is not null && salarySchedule.IsFailure)
                return Results.Problem(title: salarySchedule.Error, statusCode: StatusCodes.Status400BadRequest);

            var command = new CreateTransactionCommand(
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.Category,
                request.Recurrence,
                request.RecurrenceEndDate,
                request.RecurrenceCount,
                salarySchedule?.Value,
                request.PaymentMethod,
                request.BankAccountId,
                request.CardId);

            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Created($"/api/transactions/{result.Value!.Id}", result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/", async (
            int? month,
            int? year,
            TransactionType? type,
            TransactionCategory? category,
            int page = 1,
            int pageSize = 10,
            IMediator mediator = default!) =>
        {
            var query = new ListTransactionsQuery(month, year, type, category, page, pageSize);
            var result = await mediator.Send(query);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetTransactionQuery(id));

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTransactionRequest request, IMediator mediator) =>
        {
            var salarySchedule = MapSalarySchedule(request.SalarySchedule);
            if (salarySchedule is not null && salarySchedule.IsFailure)
                return Results.Problem(title: salarySchedule.Error, statusCode: StatusCodes.Status400BadRequest);

            var command = new UpdateTransactionCommand(
                id,
                request.Amount,
                request.Description,
                request.Date,
                request.Type,
                request.Category,
                request.Recurrence,
                request.RecurrenceEndDate,
                request.RecurrenceCount,
                salarySchedule?.Value,
                request.PaymentMethod,
                request.BankAccountId,
                request.CardId);

            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteTransactionCommand(id));

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });

        group.MapDelete("/series/{recurrenceGroupId:guid}", async (Guid recurrenceGroupId, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteSeriesCommand(recurrenceGroupId));

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
        });
    }

    private static Domain.Common.Result<SalarySchedule>? MapSalarySchedule(SalaryScheduleRequest? request)
    {
        if (request is null) return null;
        return SalarySchedule.Create(
            request.Mode, request.BusinessDayNumber, request.FixedDay,
            request.SplitFirstAmount, request.SplitFirstPercentage);
    }
}
