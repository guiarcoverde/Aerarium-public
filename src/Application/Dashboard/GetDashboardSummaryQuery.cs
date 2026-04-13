namespace Aerarium.Application.Dashboard;

using Aerarium.Domain.Common;
using Mediator;

public sealed record GetDashboardSummaryQuery(int Month, int Year) : IQuery<Result<DashboardSummaryDto>>;
