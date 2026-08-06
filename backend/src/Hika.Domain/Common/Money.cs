namespace Hika.Domain.Common;

/// <summary>
/// Amount and currency travel together so arithmetic can't silently mix currencies.
/// Persisted as an EF Core owned type (decimal(18,2) + a small currency column), never a bare decimal.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }

    public Currency Currency { get; }

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
