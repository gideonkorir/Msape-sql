using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components.Infra
{
    public static class CosmosId
    {
        public static string FromGuid(Guid id)
            => id.ToString("D", CultureInfo.InvariantCulture);
    }
}
