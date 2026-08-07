namespace Hika.Domain.Common;

/// <summary>
/// Amount and currency travel together so arithmetic can't silently mix currencies.
/// Persisted as an EF Core complex type (decimal(18,2) + a small currency column), never a
/// bare decimal.
/// </summary>
public readonly record struct Money
{
    /// <remarks>
    /// init (not a plain get-only property assigned only in the constructor body) — EF Core's
    /// complex-type materialization needs a settable path to rehydrate a Money from stored
    /// column values via its parameterless constructor, and init accessors are exactly the
    /// "settable only at construction" shape that fits both that and this type's intended
    /// immutability. A pure get-only property backed solely by constructor-body assignment
    /// left EF with no constructor it could bind confidently, which surfaced as "No suitable
    /// constructor was found" at model-build time.
    /// </remarks>
    public decimal Amount { get; init; }

    public Currency Currency { get; init; }

    public Money(decimal amount, Currency currency = Currency.ZAR)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Money amount cannot be negative.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency;
    }

    public static Money Zero(Currency currency = Currency.ZAR) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, int factor) => new(money.Amount * factor, money.Currency);

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in different currencies ({left.Currency} and {right.Currency}).");
        }
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}
