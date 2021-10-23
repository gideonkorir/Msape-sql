using Msape.BookKeeping.Data;

namespace Msape.BookKeeping.Components.Consumers.Posting
{
    public record MoneyInfo
    {
        public decimal Value { get; init; }
        public int Currency { get; init; }

        public Money ToMoney()
            => new (Currency, Value);

        public static implicit operator MoneyInfo(Money money)
            => new () {  Value = money.Value, Currency = money.Currency };
    }

}
