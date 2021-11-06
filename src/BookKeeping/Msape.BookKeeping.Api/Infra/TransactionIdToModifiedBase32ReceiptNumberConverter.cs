using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class TransactionIdToModifiedBase32ReceiptNumberConverter : ITransactionIdToReceiptNumberConverter
    {
        private readonly int _minLength;
        private readonly Random _random = new ();

        public TransactionIdToModifiedBase32ReceiptNumberConverter(int minLength = 10)
        {
            _minLength = minLength;
        }

        public string Convert(ulong transactionId)
        {
			//the bottom 5 bits are set.
			const byte mask = 31;
			const byte shift = 5;

			var value = transactionId;
			//13 comes from 64bits/5bits.
			Span<char> chars = stackalloc char[13];

			for (int i = chars.Length - 1; i >= 0; i--)
			{
				var index = (int)(value & mask);
				//shift index so that we aren't too obvious
				index = (index + i) % _chars.Length;
				chars[i] = _chars[index];
				value >>= shift;
			}
			return new String(chars);
		}

		/// <summary>
		/// This is our alphabet. I modified it from Crockford's Base32 as described
		/// here: https://en.wikipedia.org/wiki/Base32
		/// I randomly moved numbers and characters around so that it's not too obvious
		/// that the receipt is generated from a number
		/// </summary>
		private static readonly char[] _chars =
		{
			'0',
			'H',
			'2',
			'3',
			'4',
			'5',
			'W',
			'7',
			'A',
			'9',
			'8',
			'B',
			'C',
			'D',
			'E',
			'F',
			'G',
			'1',
			'J',
			'K',
			'M',
			'N',
			'O',
			'P',
			'Q',
			'R',
			'S',
			'T',
			'V',
			'6',
			'X',
			'Y',
			'Z'
		};
	}
}
