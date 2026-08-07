using Hika.Domain.Drivers;
using Shouldly;

namespace Hika.UnitTests.Domain.Drivers;

public class VehicleTests
{
    private static Vehicle NewVehicle() =>
        Vehicle.Create(Guid.NewGuid(), "Toyota", "Corolla", 2020, "White", "CA123456", 4);

    [Fact]
    public void AddPhoto_FirstPhotoMarkedPrimary_StaysPrimary()
    {
        var vehicle = NewVehicle();

        var photo = vehicle.AddPhoto("https://example.com/a.jpg", isPrimary: true);

        photo.IsPrimary.ShouldBeTrue();
        vehicle.Photos.Count(p => p.IsPrimary).ShouldBe(1);
    }

    [Fact]
    public void AddPhoto_SecondPrimaryPhoto_UnsetsTheFirst()
    {
        var vehicle = NewVehicle();
        var first = vehicle.AddPhoto("https://example.com/a.jpg", isPrimary: true);

        var second = vehicle.AddPhoto("https://example.com/b.jpg", isPrimary: true);

        first.IsPrimary.ShouldBeFalse();
        second.IsPrimary.ShouldBeTrue();
        vehicle.Photos.Count(p => p.IsPrimary).ShouldBe(1);
        vehicle.Photos.Count.ShouldBe(2);
    }

    [Fact]
    public void AddPhoto_NotMarkedPrimary_DoesNotAffectExistingPrimary()
    {
        var vehicle = NewVehicle();
        var first = vehicle.AddPhoto("https://example.com/a.jpg", isPrimary: true);

        vehicle.AddPhoto("https://example.com/b.jpg", isPrimary: false);

        first.IsPrimary.ShouldBeTrue();
        vehicle.Photos.Count(p => p.IsPrimary).ShouldBe(1);
    }

    [Fact]
    public void Update_ChangingRegistrationNumber_ResetsVerification()
    {
        var vehicle = NewVehicle();
        vehicle.MarkVerified();

        vehicle.Update("Toyota", "Corolla", 2020, "White", "GP999999", 4);

        vehicle.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public void Update_SameRegistrationNumber_KeepsVerification()
    {
        var vehicle = NewVehicle();
        vehicle.MarkVerified();

        vehicle.Update("Toyota", "Corolla", 2021, "Blue", "CA123456", 5);

        vehicle.IsVerified.ShouldBeTrue();
    }
}
