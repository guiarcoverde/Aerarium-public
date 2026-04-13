namespace Aerarium.Application;

using Aerarium.Application.Common;
using Aerarium.Application.Common.Behaviors;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient<ICategoryLocalizer, CategoryLocalizer>();
        services.AddTransient<IPaymentMethodLocalizer, PaymentMethodLocalizer>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
