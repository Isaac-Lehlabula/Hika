using Hika.Application.Chat.Dtos;

namespace Hika.Application.Chat;

public interface IChatService
{
    /// <summary>Opens a conversation for a booking that's just been accepted/claimed. Adds it to
    /// the current unit of work without saving — same pattern as INotificationDispatcher — so
    /// BookingService can fold this into the same SaveChangesAsync call as the booking's own
    /// status change. A no-op if a conversation already exists for this booking.</summary>
    Task OpenForBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    /// <summary>Closes a booking's conversation, if one exists and is still open — called from
    /// every terminal transition a booking can reach after acceptance (completed, payment
    /// failed, cancelled). Same no-save-here contract as OpenForBookingAsync.</summary>
    Task CloseForBookingAsync(Guid bookingId, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if no conversation exists yet for this booking, or if
    /// the caller isn't the booking's passenger or the trip's driver.</summary>
    Task<ConversationResponse> GetAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken);

    Task<ChatMessageResponse> SendMessageAsync(Guid callerId, Guid bookingId, string body, CancellationToken cancellationToken);
}
