using Microsoft.AspNetCore.Identity;

namespace Hika.Infrastructure.Identity;

/// <summary>
/// The ASP.NET Core Identity persistence model — deliberately thin (auth-only: email,
/// password hash, lockout, security stamp). Everything product-facing (name, photo, phone,
/// rating) lives in the Domain's UserProfile, keyed by the same Guid. See
/// docs/architecture.md §4 for why this split exists.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
