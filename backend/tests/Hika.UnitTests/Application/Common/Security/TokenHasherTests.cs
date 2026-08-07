using Hika.Application.Common.Security;
using Shouldly;

namespace Hika.UnitTests.Application.Common.Security;

public class TokenHasherTests
{
    [Fact]
    public void Hash_SameInput_ProducesSameHash()
    {
        TokenHasher.Hash("abc123").ShouldBe(TokenHasher.Hash("abc123"));
    }

    [Fact]
    public void Hash_DifferentInput_ProducesDifferentHash()
    {
        TokenHasher.Hash("abc123").ShouldNotBe(TokenHasher.Hash("abc124"));
    }

    [Fact]
    public void Hash_NeverReturnsTheRawValue()
    {
        TokenHasher.Hash("my-secret-token").ShouldNotBe("my-secret-token");
    }
}
