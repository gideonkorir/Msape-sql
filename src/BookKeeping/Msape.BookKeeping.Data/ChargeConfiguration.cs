using System;
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

    public class ChargeData
    {
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
}
