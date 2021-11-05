using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class TransactionIdToReceiptNumberConverterOptions
    {
        public string Pattern { get; set; }
    }
    public class TransactionIdToReceiptNumberConverter : ITransactionIdToReceiptNumberConverter
    {
        public const char Digit = '#', Letter = '@';

        private readonly string _pattern;

        public string Pattern => _pattern;

        public TransactionIdToReceiptNumberConverter(IOptions<TransactionIdToReceiptNumberConverterOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.Value?.Pattern))
            {
                throw new ArgumentException($"The pattern must not be null, empty or whitespace");
            }
            //validate the pattern
            _pattern = options.Value.Pattern;
            ulong coverage = 1;
            foreach (var c in _pattern)
            {
                switch (c)
                {
                    case Digit:
                        coverage *= 10;
                        break;
                    case Letter:
                        coverage *= 25;
                        break;
                }
            }
            if (coverage == 1)
                throw new ArgumentException($"The pattern must include digit and letter patterns otherwise it will just be a constant");
        }

        public string Convert(ulong transactionId)
        {
            return String.Create<CreateState>(_pattern.Length, new CreateState { Pattern = _pattern, TransactionId = transactionId }, (span, state) =>
            {
                var number = state.TransactionId;
                for (int i = 0; i < state.Pattern.Length; i++)
                {
                    switch (state.Pattern[i])
                    {
                        case Digit:
                            span[^(i + 1)] = (char)('0' + number % 10);
                            number /= 10;
                            break;
                        case Letter:
                            span[^(i + 1)] = (char)('A' + number % 26);
                            number /= 26;
                            break;
                        default:
                            span[^(i + 1)] = state.Pattern[i];
                            break;
                    }
                }
            });
        }

        private struct CreateState
        {
            public string Pattern { get; set; }
            public ulong TransactionId { get; set; }
        }
    }
}
