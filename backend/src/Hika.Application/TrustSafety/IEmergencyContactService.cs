using Hika.Application.TrustSafety.Dtos;

namespace Hika.Application.TrustSafety;

public interface IEmergencyContactService
{
    Task<EmergencyContactResponse> CreateAsync(Guid userId, EmergencyContactRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmergencyContactResponse>> GetMyContactsAsync(Guid userId, CancellationToken cancellationToken);

    Task<EmergencyContactResponse> UpdateAsync(
        Guid userId, Guid contactId, EmergencyContactRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid contactId, CancellationToken cancellationToken);
}
