using Hika.Domain.Notifications;
using Shouldly;

namespace Hika.UnitTests.Domain.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_SetsChannelToInAppAndStatusToSent()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.BookingRequested, "New request", Guid.NewGuid());

        notification.Channel.ShouldBe(NotificationChannel.InApp);
        notification.Status.ShouldBe(NotificationStatus.Sent);
        notification.ReadAtUtc.ShouldBeNull();
    }

    [Fact]
    public void MarkRead_SentNotification_SetsStatusAndTimestamp()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.BookingAccepted, "Accepted", null);

        notification.MarkRead();

        notification.Status.ShouldBe(NotificationStatus.Read);
        notification.ReadAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkRead_AlreadyRead_IsIdempotent()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.BookingAccepted, "Accepted", null);
        notification.MarkRead();
        var firstReadAt = notification.ReadAtUtc;

        notification.MarkRead();

        notification.ReadAtUtc.ShouldBe(firstReadAt);
    }
}
