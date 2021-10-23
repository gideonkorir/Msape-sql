using Microsoft.Azure.Cosmos;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Infra
{
    public interface ICosmosAccount
    {
        Container Transactions { get; }
        Container Accounts { get; }
        Container AccountNumbers { get; }
    }
}
