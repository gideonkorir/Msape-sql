using MassTransit;
using MassTransit.Topology.Topologies;
using Msape.BookKeeping.Components.Consumers.Posting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Components
{
    public static class MTConfigurationInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            GlobalTopology.Send.UseSessionIdFormatter<PostTransactionToDest>(x => x.Message.DestAccount.Id.ToString("D", CultureInfo.InvariantCulture));
        }
    }
}
