using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.TrustSafety.Dtos;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.TrustSafety;

public sealed class EmergencyContactService(IAppDbContext db) : IEmergencyContactService
{
    public async Task<EmergencyContactResponse> CreateAsync(
        Guid userId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var contact = EmergencyContact.Create(userId, request.Name, request.PhoneNumber, request.Relationship);

        db.EmergencyContacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(contact);
    }

    public async Task<IReadOnlyList<EmergencyContactResponse>> GetMyContactsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var contacts = await db.EmergencyContacts
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return contacts.Select(ToResponse).ToList();
    }

    public async Task<EmergencyContactResponse> UpdateAsync(
        Guid userId, Guid contactId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var contact = await LoadOwnedAsync(userId, contactId, cancellationToken);

        contact.Update(request.Name, request.PhoneNumber, request.Relationship);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(contact);
    }

    public async Task DeleteAsync(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await LoadOwnedAsync(userId, contactId, cancellationToken);

        db.EmergencyContacts.Remove(contact);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<EmergencyContact> LoadOwnedAsync(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await db.EmergencyContacts.FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyContact), contactId);

        if (contact.UserId != userId)
        {
            throw new NotFoundException(nameof(EmergencyContact), contactId);
        }

        return contact;
    }

    private static EmergencyContactResponse ToResponse(EmergencyContact contact) => new()
    {
        Id = contact.Id,
        Name = contact.Name,
        PhoneNumber = contact.PhoneNumber,
        Relationship = contact.Relationship,
    };
}
