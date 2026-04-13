namespace Aerarium.Application.Common;

using Aerarium.Application.Resources;
using Aerarium.Domain.Enums;
using Microsoft.Extensions.Localization;

public sealed class CategoryLocalizer(IStringLocalizer<Categories> localizer) : ICategoryLocalizer
{
    public string GetDisplayName(TransactionCategory category)
    {
        var localized = localizer[category.ToString()];
        return localized.ResourceNotFound ? category.ToString() : localized.Value;
    }
}
