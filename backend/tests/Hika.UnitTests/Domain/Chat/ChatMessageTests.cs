using Hika.Domain.Chat;
using Shouldly;

namespace Hika.UnitTests.Domain.Chat;

public class ChatMessageTests
{
    [Fact]
    public void Send_TrimsWhitespaceAndSetsSentAtUtc()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        var message = ChatMessage.Send(conversationId, senderId, "  Running 10 minutes late  ");

        message.ConversationId.ShouldBe(conversationId);
        message.SenderUserId.ShouldBe(senderId);
        message.Body.ShouldBe("Running 10 minutes late");
        message.SentAtUtc.ShouldNotBe(default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Send_EmptyOrWhitespaceBody_Throws(string body)
    {
        Should.Throw<ArgumentException>(() => ChatMessage.Send(Guid.NewGuid(), Guid.NewGuid(), body));
    }

    [Fact]
    public void Send_BodyExceedingMaxLength_Throws()
    {
        var tooLong = new string('a', ChatMessage.MaxBodyLength + 1);

        Should.Throw<ArgumentException>(() => ChatMessage.Send(Guid.NewGuid(), Guid.NewGuid(), tooLong));
    }

    [Fact]
    public void Send_BodyAtMaxLength_Succeeds()
    {
        var atLimit = new string('a', ChatMessage.MaxBodyLength);

        var message = ChatMessage.Send(Guid.NewGuid(), Guid.NewGuid(), atLimit);

        message.Body.Length.ShouldBe(ChatMessage.MaxBodyLength);
    }
}
