using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.Topology.Topologies;
using Msape.BookKeeping.InternalContracts;
using System.Globalization;
using System.Linq;

namespace Msape.BookKeeping.Api
{
    public static class MTModuleInitializer
    {
        public static void Initialize(IServiceBusBusFactoryConfigurator configurator)
        {
            var method = typeof(MTModuleInitializer).GetMethod(nameof(ConfigureTransactionMessage), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            var generic = typeof(PostTransaction)
                .Assembly
                .GetExportedTypes()
                .Where(c => c.IsAssignableTo(typeof(PostTransaction)) && !c.IsAbstract)
                .Select(c => method.MakeGenericMethod(c));
            var arg = new object[] { configurator };

            foreach(var g in generic)
            {
                g.Invoke(null, arg);
            }
        }

        private static void ConfigureTransactionMessage<T>(IServiceBusBusFactoryConfigurator configurator)
            where T : PostTransaction
        {
            configurator.Send<T>(configure =>
            {
                configure.UseSessionIdFormatter(p => p.Message.SourceAccount.Id.ToString("D", CultureInfo.InvariantCulture));
            });
        }
    }
}
