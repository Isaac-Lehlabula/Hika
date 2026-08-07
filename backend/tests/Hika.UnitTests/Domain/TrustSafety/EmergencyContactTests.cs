using Hika.Domain.TrustSafety;
using Shouldly;

namespace Hika.UnitTests.Domain.TrustSafety;

public class EmergencyContactTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var userId = Guid.NewGuid();

        var contact = EmergencyContact.Create(userId, "Naledi Dlamini", "+27821234567", "Sister");

        contact.UserId.ShouldBe(userId);
        contact.Name.ShouldBe("Naledi Dlamini");
        contact.PhoneNumber.ShouldBe("+27821234567");
        contact.Relationship.ShouldBe("Sister");
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var contact = EmergencyContact.Create(Guid.NewGuid(), "Naledi Dlamini", "+27821234567", "Sister");

        contact.Update("Naledi M", "+27829999999", "Mother");

        contact.Name.ShouldBe("Naledi M");
        contact.PhoneNumber.ShouldBe("+27829999999");
        contact.Relationship.ShouldBe("Mother");
    }
}
