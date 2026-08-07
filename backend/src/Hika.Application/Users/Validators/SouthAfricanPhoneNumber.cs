using System.Text.RegularExpressions;

namespace Hika.Application.Users.Validators;

/// <summary>E.164 South African mobile numbers, e.g. +27821234567.</summary>
public static partial class SouthAfricanPhoneNumber
{
    [GeneratedRegex(@"^\+27[1-9]\d{8}$")]
    public static partial Regex Pattern();
}
