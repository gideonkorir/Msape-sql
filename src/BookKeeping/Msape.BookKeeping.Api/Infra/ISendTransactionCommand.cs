using Msape.BookKeeping.InternalContracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public interface ISendTransactionCommand
    {
        Task Send(PostTransaction command, CancellationToken cancellationToken);
    }
}
