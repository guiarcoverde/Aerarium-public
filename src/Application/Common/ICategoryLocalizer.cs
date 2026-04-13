namespace Aerarium.Application.Common;

using Aerarium.Domain.Enums;

public interface ICategoryLocalizer
{
    string GetDisplayName(TransactionCategory category);
}
