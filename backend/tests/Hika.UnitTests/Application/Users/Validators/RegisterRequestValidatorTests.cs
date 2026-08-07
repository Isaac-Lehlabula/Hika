using Hika.Application.Users.Dtos;
using Hika.Application.Users.Validators;
using Shouldly;

namespace Hika.UnitTests.Application.Users.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new()
    {
        Email = "thabo@example.com",
        Password = "Passw0rd123",
        FirstName = "Thabo",
        LastName = "Nkosi",
        PhoneNumber = "+27821234567",
    };

    [Fact]
    public void Valid_Request_PassesValidation()
    {
        _validator.Validate(Valid()).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Invalid_Password_FailsValidation(string password)
    {
        var request = Valid() with { Password = password };

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("0821234567")] // missing +27
    [InlineData("+1234567890")] // wrong country code
    [InlineData("+2782123")] // too short
    public void Invalid_SouthAfricanPhoneNumber_FailsValidation(string phone)
    {
        var request = Valid() with { PhoneNumber = phone };

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void NullPhoneNumber_IsAllowed()
    {
        var request = Valid() with { PhoneNumber = null };

        _validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void InvalidEmail_FailsValidation()
    {
        var request = Valid() with { Email = "not-an-email" };

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }
}
