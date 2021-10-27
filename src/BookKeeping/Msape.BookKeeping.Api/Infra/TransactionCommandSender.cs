using MassTransit;
using Msape.BookKeeping.InternalContracts;
using Msape.BookKeeping.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class TransactionCommandSender : ISendTransactionCommand
    {
        private readonly IBus _bus;
        private readonly Dictionary<AccountType, Uri> _accountTypeQueueMapping;

        public TransactionCommandSender(IBus bus, Func<AccountType, string> accountTypeToQueueName)
        {
            if (accountTypeToQueueName is null)
            {
                throw new ArgumentNullException(nameof(accountTypeToQueueName));
            }

            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            var enumValues = Enum.GetValues<AccountType>();
            _accountTypeQueueMapping = new Dictionary<AccountType, Uri>(enumValues.Length);
            foreach (AccountType type in enumValues)
                _accountTypeQueueMapping.Add(type, new Uri($"queue:{accountTypeToQueueName(type)}"));
        }

        public async Task Send(PostTransaction command, CancellationToken cancellationToken)
        {
            var sendEndpoint = await _bus.GetSendEndpoint(
                address: _accountTypeQueueMapping[command.SourceAccount.AccountType]
                ).ConfigureAwait(false);
            await sendEndpoint.Send(command, cancellationToken).ConfigureAwait(false);
        }
    }
}
