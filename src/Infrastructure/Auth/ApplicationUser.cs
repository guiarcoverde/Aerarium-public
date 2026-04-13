namespace Aerarium.Infrastructure.Auth;

using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}
