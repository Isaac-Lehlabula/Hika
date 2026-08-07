using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Common.Storage;
using Hika.Application.Drivers.Dtos;
using Hika.Domain.Drivers;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Drivers;

public sealed class DriverProfileService(IAppDbContext db, IFileStorage fileStorage) : IDriverProfileService
{
    public async Task<DriverProfileResponse> CreateOrUpdateAsync(
        Guid userId, CreateOrUpdateDriverProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await db.DriverProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null)
        {
            profile = DriverProfile.Create(userId, request.LicenseNumber, request.LicenseExpiryDate);
            db.DriverProfiles.Add(profile);
        }
        else
        {
            profile.UpdateLicense(request.LicenseNumber, request.LicenseExpiryDate);
        }

        await db.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(profile, cancellationToken);
    }

    public async Task<DriverProfileResponse> GetOwnAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.DriverProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(DriverProfile), userId);

        return await BuildResponseAsync(profile, cancellationToken);
    }

    public async Task SubmitLicenseVerificationAsync(
        Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var profile = await db.DriverProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(DriverProfile), userId);

        var url = await fileStorage.SaveAsync(content, "verification-documents", fileName, contentType, cancellationToken);

        var verification = await db.Verifications
            .Where(v => v.SubjectType == VerificationSubjectType.User
                && v.SubjectId == userId
                && v.Type == VerificationType.DriverLicense)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification is null)
        {
            verification = Verification.CreateAndSubmit(VerificationSubjectType.User, userId, VerificationType.DriverLicense, url);
            db.Verifications.Add(verification);
        }
        else
        {
            verification.Submit(url);
        }

        profile.MarkUnverified();

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<DriverProfileResponse> BuildResponseAsync(DriverProfile profile, CancellationToken cancellationToken)
    {
        var verification = await db.Verifications
            .Where(v => v.SubjectType == VerificationSubjectType.User
                && v.SubjectId == profile.Id
                && v.Type == VerificationType.DriverLicense)
            .OrderByDescending(v => v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new DriverProfileResponse
        {
            UserId = profile.Id,
            LicenseNumber = profile.LicenseNumber,
            LicenseExpiryDate = profile.LicenseExpiryDate,
            IsVerifiedDriver = profile.IsVerifiedDriver,
            VerificationStatus = (verification?.Status ?? VerificationStatus.NotSubmitted).ToString(),
        };
    }
}
