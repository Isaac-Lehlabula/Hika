using Hika.Domain.Common;

namespace Hika.Domain.Drivers;

public sealed class Vehicle : AuditableEntity
{
    public Guid DriverProfileId { get; private set; }

    public string Make { get; private set; }

    public string Model { get; private set; }

    public int Year { get; private set; }

    public string Color { get; private set; }

    public string RegistrationNumber { get; private set; }

    public int SeatCapacity { get; private set; }

    /// <summary>Cached from the latest Verification of type VehicleRegistration.</summary>
    public bool IsVerified { get; private set; }

    private readonly List<VehiclePhoto> _photos = [];
    public IReadOnlyCollection<VehiclePhoto> Photos => _photos.AsReadOnly();

    private Vehicle()
    {
        Make = string.Empty;
        Model = string.Empty;
        Color = string.Empty;
        RegistrationNumber = string.Empty;
    }

    private Vehicle(
        Guid driverProfileId, string make, string model, int year, string color,
        string registrationNumber, int seatCapacity)
    {
        DriverProfileId = driverProfileId;
        Make = make;
        Model = model;
        Year = year;
        Color = color;
        RegistrationNumber = registrationNumber;
        SeatCapacity = seatCapacity;
        IsVerified = false;
    }

    public static Vehicle Create(
        Guid driverProfileId, string make, string model, int year, string color,
        string registrationNumber, int seatCapacity) =>
        new(driverProfileId, make, model, year, color, registrationNumber, seatCapacity);

    public void Update(string make, string model, int year, string color, string registrationNumber, int seatCapacity)
    {
        if (registrationNumber != RegistrationNumber)
        {
            // A changed registration number is effectively a different vehicle for
            // verification purposes — the prior verification no longer applies.
            IsVerified = false;
        }

        Make = make;
        Model = model;
        Year = year;
        Color = color;
        RegistrationNumber = registrationNumber;
        SeatCapacity = seatCapacity;
    }

    public void MarkVerified() => IsVerified = true;

    public void MarkUnverified() => IsVerified = false;

    public VehiclePhoto AddPhoto(string url, bool isPrimary)
    {
        if (isPrimary)
        {
            foreach (var existing in _photos)
            {
                existing.SetPrimary(false);
            }
        }

        var photo = new VehiclePhoto(Id, url, isPrimary, _photos.Count);
        _photos.Add(photo);
        return photo;
    }
}
