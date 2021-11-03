using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Controllers
{
    internal static class ChargeDataUtil
    {
        private static readonly ChargeType[] _chargeTypes = Enum.GetValues<ChargeType>();

        public static TransactionType ChargeTypeToTransactionType(ChargeType chargeType)
        {
            return chargeType switch
            {
                ChargeType.SystemCharge => TransactionType.TransactionCharge,
                ChargeType.AgentFees => TransactionType.AgentFees,
                _ => throw new InvalidOperationException($"Unsupported {chargeType} to TransactionType mapping")
            };
        }

        public static void Validate(TransactionType transactionType, Currency currency, List<ChargeData> charges)
        {
            if (charges.Count <= 1)
                return;
            if (charges.Count == 2)
            {
                if (charges[0].ChargeType != charges[1].ChargeType)
                    return;
                else
                    Throw(transactionType, currency, charges[0].ChargeType);
            }
            
            foreach(var chargeType in _chargeTypes)
            {
                int count = charges.Count(c => c.ChargeType == chargeType);
                if (count > 1)
                    Throw(transactionType, currency, chargeType);
            }

            static void Throw(TransactionType transactionType, Currency currency, ChargeType chargeType)
            {
                throw new InvalidOperationException($"Invalid charge configuration for [Type: {transactionType}, Currency: {currency}]. Charge type {chargeType} was defined more than once");
            }
        }
    }
}
