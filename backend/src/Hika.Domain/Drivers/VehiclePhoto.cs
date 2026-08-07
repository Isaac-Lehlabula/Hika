using Hika.Domain.Common;

namespace Hika.Domain.Drivers;

public sealed class VehiclePhoto : Entity
{
    public Guid VehicleId { get; private set; }

    public string Url { get; private set; }

    public bool IsPrimary { get; private set; }

    public int SortOrder { get; private set; }

    private VehiclePhoto()
    {
        Url = string.Empty;
    }

    internal VehiclePhoto(Guid vehicleId, string url, bool isPrimary, int sortOrder)
    {
        VehicleId = vehicleId;
        Url = url;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
