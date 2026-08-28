using Hika.Application.Chat.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Notifications;
using Hika.Domain.Bookings;
using Hika.Domain.Chat;
using Hika.Domain.Notifications;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Chat;

public sealed class ChatService(IAppDbContext db, INotificationDispatcher notificationDispatcher) : IChatService
{
    public async Task OpenForBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var alreadyExists = db.Conversations.Local.Any(c => c.BookingId == bookingId)
            || await db.Conversations.AnyAsync(c => c.BookingId == bookingId, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        db.Conversations.Add(Conversation.Open(bookingId));
    }

    public async Task CloseForBookingAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var conversation = db.Conversations.Local.FirstOrDefault(c => c.BookingId == bookingId)
            ?? await db.Conversations.FirstOrDefaultAsync(c => c.BookingId == bookingId, cancellationToken);

        conversation?.Close();
    }

    public async Task<ConversationResponse> GetAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken)
    {
        var (conversation, _) = await LoadForParticipantAsync(callerId, bookingId, cancellationToken);

        var messages = await db.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(cancellationToken);

        return ToResponse(conversation, messages, callerId);
    }

    public async Task<ChatMessageResponse> SendMessageAsync(Guid callerId, Guid bookingId, string body, CancellationToken cancellationToken)
    {
        var (conversation, booking) = await LoadForParticipantAsync(callerId, bookingId, cancellationToken);

        if (!conversation.IsOpen)
        {
            throw new ConflictException("This conversation is closed.");
        }

        var message = ChatMessage.Send(conversation.Id, callerId, body);
        db.ChatMessages.Add(message);

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken)
            ?? throw new InvalidOperationException("Booking references a trip that no longer exists.");
        var recipientId = callerId == booking.PassengerUserId ? trip.DriverProfileId : booking.PassengerUserId;

        await notificationDispatcher.DispatchAsync(recipientId, NotificationType.NewChatMessage, "New message about your trip.", bookingId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new ChatMessageResponse
        {
            Id = message.Id,
            SenderUserId = message.SenderUserId,
            IsMine = true,
            Body = message.Body,
            SentAtUtc = message.SentAtUtc,
        };
    }

    private async Task<(Conversation Conversation, Booking Booking)> LoadForParticipantAsync(
        Guid callerId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        if (booking.PassengerUserId != callerId)
        {
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken);
            if (trip is null || trip.DriverProfileId != callerId)
            {
                throw new NotFoundException(nameof(Booking), bookingId);
            }
        }

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.BookingId == bookingId, cancellationToken)
            ?? throw new NotFoundException("Conversation for booking", bookingId);

        return (conversation, booking);
    }

    private static ConversationResponse ToResponse(Conversation conversation, IReadOnlyList<ChatMessage> messages, Guid callerId) => new()
    {
        Id = conversation.Id,
        BookingId = conversation.BookingId,
        IsOpen = conversation.IsOpen,
        Messages = messages.Select(m => new ChatMessageResponse
        {
            Id = m.Id,
            SenderUserId = m.SenderUserId,
            IsMine = m.SenderUserId == callerId,
            Body = m.Body,
            SentAtUtc = m.SentAtUtc,
        }).ToList(),
    };
}
