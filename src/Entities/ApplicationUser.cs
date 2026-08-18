using Microsoft.AspNetCore.Identity;

namespace Entities;

/// <summary>docs/MIMARI.md · A-09/A-11/A-28: Identity kullanıcısı, int anahtarlı, davranışsız.</summary>
public sealed class ApplicationUser : IdentityUser<int>
{
}
