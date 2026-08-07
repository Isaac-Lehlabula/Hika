using Hika.Domain.Common;

namespace Hika.Domain.Drivers;

/// <summary>
/// 1:1 with a user (shared primary key, same pattern as UserProfile — see
/// docs/architecture.md §4). Any user can become a driver; having a DriverProfile (and at
/// least one verified vehicle) is what unlocks posting trips, not a separate account type.
/// </summary>
public sealed class DriverProfile : Entity
{
    public string LicenseNumber { get; private set; }

    public DateOnly LicenseExpiryDate { get; private set; }

    /// <summary>
    /// Cached from the latest Verification of type DriverLicense — cheap to read on every
    /// trip-search result; the Verification row remains the source of truth and audit trail.
    /// </summary>
    public bool IsVerifiedDriver { get; private set; }

    private DriverProfile()
    {
        LicenseNumber = string.Empty;
    }

    private DriverProfile(Guid userId, string licenseNumber, DateOnly licenseExpiryDate)
    {
        Id = userId;
        LicenseNumber = licenseNumber;
        LicenseExpiryDate = licenseExpiryDate;
        IsVerifiedDriver = false;
    }

    public static DriverProfile Create(Guid userId, string licenseNumber, DateOnly licenseExpiryDate) =>
        new(userId, licenseNumber, licenseExpiryDate);

    public void UpdateLicense(string licenseNumber, DateOnly licenseExpiryDate)
    {
        // Changing license details invalidates any prior verification — the new details
        // haven't been reviewed yet.
        if (licenseNumber != LicenseNumber || licenseExpiryDate != LicenseExpiryDate)
        {
            IsVerifiedDriver = false;
        }

        LicenseNumber = licenseNumber;
        LicenseExpiryDate = licenseExpiryDate;
    }

    public void MarkVerified() => IsVerifiedDriver = true;

    public void MarkUnverified() => IsVerifiedDriver = false;
}
