using Microsoft.Azure.Cosmos;

namespace Msape.BookKeeping.Api.Infra
{ 
    public interface ICosmosAccount
    {
        Container Transactions { get; }
        Container Accounts { get; }
        Container AccountNumbers { get; }
    }
}
