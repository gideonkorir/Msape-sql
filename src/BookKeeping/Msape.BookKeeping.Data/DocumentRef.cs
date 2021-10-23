namespace Msape.BookKeeping.Data
{
    public record DocumentRef
    {
        public string Id { get; init; }
        public string PartitionKey { get; init; }
    }

    public record DocumentRef<T> where T : struct
    {
        public T Id { get; init; }
        public string PartitionKey { get; init; }
    }
}
