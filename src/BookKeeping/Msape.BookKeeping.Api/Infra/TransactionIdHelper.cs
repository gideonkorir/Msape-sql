using System;
using System.Linq;

namespace Msape.BookKeeping.Api.Infra
{
    public static class TransactionIdHelper
    {
        public static Guid ToGuid(long transactionId)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            //copy ticks
            CopyBytes(guidBytes, DateTime.UtcNow.Ticks);
            CopyBytes(guidBytes[8..], transactionId);
            return new Guid(guidBytes);


            static void CopyBytes(Span<byte> dest, long value)
            {
                var bytes = BitConverter.GetBytes(value);
                //use the little endian format always to reverse the bytes
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                //repeat the same process
                bytes.CopyTo(dest);
            }
        }

        public static long GetTransactionId(Guid id)
        {
            Span<byte> bytes = stackalloc byte[16];
            id.TryWriteBytes(bytes);
            var idBytes = bytes[8..];
            if(!BitConverter.IsLittleEndian)
            {
                //the data is in little endian format
                idBytes.Reverse();
            }
            return BitConverter.ToInt64(idBytes);
        }
    }
}
