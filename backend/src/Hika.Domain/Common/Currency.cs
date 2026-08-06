namespace Hika.Domain.Common;

/// <summary>
/// Modeled as an enum (not a bare string/hardcoded constant) so the schema is
/// already shaped for multi-currency even though ZAR is the only value used today.
/// </summary>
public enum Currency
{
    ZAR,
}
