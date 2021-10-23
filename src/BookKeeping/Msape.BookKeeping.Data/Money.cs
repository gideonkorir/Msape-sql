using System;

namespace Msape.BookKeeping.Data
{
    public struct Money : IEquatable<Money>
    {
        public int Currency { get; private set; }
        public decimal Value { get; private set; }

        public Money(int currency, decimal value)
        {
            Currency = currency;
            Value = value;
        }

        private Money Add(Money other)
        {
            CheckCurrency(other.Currency, "Can not add money with differing currency");
            return new Money(Currency, Value + other.Value);
        }

        private Money Subtract(Money other)
        {
            CheckCurrency(other.Currency, "Can not subtract money with differing currency");
            return new Money(Currency, Value - other.Value);
        }

        private void CheckCurrency(int otherCurrency, string message)
        {
            if (Currency != otherCurrency)
                throw new ArgumentException($"{message}. This currency {Currency}, Other currency {otherCurrency}");
        }

        public bool Equals(Money other)
            => Currency == other.Currency && Value == other.Value;

        public override bool Equals(object obj)
        {
            if (obj is not null && obj is Money money)
                return Equals(money);
            return false;
        }

        public override int GetHashCode()
            => HashCode.Combine(Currency, Value);

        public bool HasCurrency(int currency)
            => Currency == currency;

        public bool HasCurrency(Money other)
            => HasCurrency(other.Currency);


        public override string ToString()
            => $"{Currency}. {Value}";

        public static Money Zero(int currency)
            => new(currency, 0);

        public static Money Zero(Money source)
            => new(source.Currency, 0);

        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !(left == right);
        }

        public static Money operator +(Money left, Money right)
            => left.Add(right);

        public static Money operator +(Money left, decimal value)
            => left.Add(new Money(left.Currency, value));

        public static Money operator -(Money left, Money right)
            => left.Subtract(right);

        public static Money operator -(Money left, decimal right)
            => left.Subtract(new Money(left.Currency, right));
    }
}
