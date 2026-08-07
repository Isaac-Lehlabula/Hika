using Hika.Application.Common.Exceptions;
using Hika.Application.Notifications;
using Hika.Domain.Notifications;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Notifications;

public class NotificationServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_db);
    }

    private async Task<Guid> SeedNotificationAsync(Guid userId, NotificationType type = NotificationType.BookingRequested)
    {
        var notification = Notification.Create(userId, type, "Test message", null);
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(CancellationToken.None);
        return notification.Id;
    }

    [Fact]
    public async Task GetMyNotificationsAsync_OnlyReturnsCallersOwn()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationAsync(userId);
        await SeedNotificationAsync(Guid.NewGuid());

        var result = await _sut.GetMyNotificationsAsync(userId, 1, 20, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetMyNotificationsAsync_OrdersNewestFirst()
    {
        var userId = Guid.NewGuid();
        await SeedNotificationAsync(userId, NotificationType.BookingRequested);
        await Task.Delay(10);
        var secondId = await SeedNotificationAsync(userId, NotificationType.BookingAccepted);

        var result = await _sut.GetMyNotificationsAsync(userId, 1, 20, CancellationToken.None);

        result.Items[0].Id.ShouldBe(secondId);
    }

    [Fact]
    public async Task MarkReadAsync_OwnNotification_SetsStatusToRead()
    {
        var userId = Guid.NewGuid();
        var notificationId = await SeedNotificationAsync(userId);

        await _sut.MarkReadAsync(userId, notificationId, CancellationToken.None);

        var result = await _sut.GetMyNotificationsAsync(userId, 1, 20, CancellationToken.None);
        result.Items.Single().Status.ShouldBe("Read");
    }

    [Fact]
    public async Task MarkReadAsync_NotOwnNotification_ThrowsNotFound()
    {
        var notificationId = await SeedNotificationAsync(Guid.NewGuid());

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.MarkReadAsync(Guid.NewGuid(), notificationId, CancellationToken.None));
    }
}
