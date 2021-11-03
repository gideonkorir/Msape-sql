using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Data
{
    public class ChargeConfiguration
    {
        public TransactionType TransactionType { get; set; }
        public Currency Currency { get; set; }
        public List<ChargeData> Data { get; set; } = new List<ChargeData>();
    }
}
