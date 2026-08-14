using Hika.Infrastructure.Payments.Ozow;
using Shouldly;

namespace Hika.UnitTests.Infrastructure.Payments.Ozow;

public class OzowHashHelperTests
{
    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        var hash1 = OzowHashHelper.ComputeHash(["a", "b", "c"], "secret");
        var hash2 = OzowHashHelper.ComputeHash(["a", "b", "c"], "secret");

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void ComputeHash_IsCaseInsensitiveOnInput()
    {
        var lower = OzowHashHelper.ComputeHash(["abc"], "secret");
        var upper = OzowHashHelper.ComputeHash(["ABC"], "SECRET");

        lower.ShouldBe(upper);
    }

    [Fact]
    public void ComputeHash_FieldOrderMatters()
    {
        var hash1 = OzowHashHelper.ComputeHash(["a", "b"], "secret");
        var hash2 = OzowHashHelper.ComputeHash(["b", "a"], "secret");

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentPrivateKey_ProducesDifferentHash()
    {
        var hash1 = OzowHashHelper.ComputeHash(["a", "b"], "secret1");
        var hash2 = OzowHashHelper.ComputeHash(["a", "b"], "secret2");

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_NullValuesTreatedAsEmptyString()
    {
        var withNull = OzowHashHelper.ComputeHash(["a", null, "b"], "secret");
        var withEmpty = OzowHashHelper.ComputeHash(["a", "", "b"], "secret");

        withNull.ShouldBe(withEmpty);
    }

    [Fact]
    public void ComputeHash_ReturnsLowercase128CharHex()
    {
        var hash = OzowHashHelper.ComputeHash(["test"], "key");

        hash.ShouldBe(hash.ToLowerInvariant());
        hash.Length.ShouldBe(128);
    }
}
