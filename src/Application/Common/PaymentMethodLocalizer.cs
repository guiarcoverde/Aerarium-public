namespace Aerarium.Application.Common;

using Aerarium.Application.Resources;
using Aerarium.Domain.Enums;
using Microsoft.Extensions.Localization;

public sealed class PaymentMethodLocalizer(IStringLocalizer<PaymentMethods> localizer) : IPaymentMethodLocalizer
{
    public string GetDisplayName(PaymentMethod method)
    {
        var localized = localizer[method.ToString()];
        return localized.ResourceNotFound ? method.ToString() : localized.Value;
    }
}
