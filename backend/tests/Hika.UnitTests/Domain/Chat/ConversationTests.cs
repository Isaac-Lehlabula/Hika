using Hika.Domain.Chat;
using Shouldly;

namespace Hika.UnitTests.Domain.Chat;

public class ConversationTests
{
    [Fact]
    public void Open_CreatesAnOpenConversationForTheBooking()
    {
        var bookingId = Guid.NewGuid();

        var conversation = Conversation.Open(bookingId);

        conversation.BookingId.ShouldBe(bookingId);
        conversation.IsOpen.ShouldBeTrue();
        conversation.ClosedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Close_OpenConversation_SetsClosedAtAndIsOpenBecomesFalse()
    {
        var conversation = Conversation.Open(Guid.NewGuid());

        conversation.Close();

        conversation.IsOpen.ShouldBeFalse();
        conversation.ClosedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Close_AlreadyClosedConversation_DoesNotMoveClosedAtUtc()
    {
        var conversation = Conversation.Open(Guid.NewGuid());
        conversation.Close();
        var firstClosedAt = conversation.ClosedAtUtc;

        conversation.Close();

        conversation.ClosedAtUtc.ShouldBe(firstClosedAt);
    }
}
