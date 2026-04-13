namespace Aerarium.Application.Common;

using Aerarium.Domain.Enums;

public interface IPaymentMethodLocalizer
{
    string GetDisplayName(PaymentMethod method);
}
