﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Api.Models;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.Data.EF;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    public partial class TransactionsController : ControllerBase
    {
        private static readonly string SystemAcc = "SYSTEM";

        private readonly BookKeepingContext _bookKeepingContext;
        private readonly ISubjectCache _subjectCache;

        public TransactionsController(BookKeepingContext bookKeepingContext, ISubjectCache subjectCache)
        {
            _bookKeepingContext = bookKeepingContext ?? throw new ArgumentNullException(nameof(bookKeepingContext));
            _subjectCache = subjectCache ?? throw new ArgumentNullException(nameof(subjectCache));
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get([FromRoute] long id)
        {
            var response = await _bookKeepingContext.Transactions
                    .FindAsync(new object[] { id }, HttpContext.RequestAborted)
                    .ConfigureAwait(false);

            return response != null
                ? Ok(TransactionApiModel.Create(response))
                : NotFound(new
                {
                    ErrorMessage = $"Transaction with id {id} was not found"
                });
        }

        [HttpPut("agentfloattopup")]
        public async Task<IActionResult> AgentFloatTopup(AgentFloatTopupApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var agentSubject = await GetSubjectAsync(model.AgentNumber, AccountType.AgentFloat).ConfigureAwait(false);
            var systemSubject = await GetSubjectAsync(SystemAcc, AccountType.SystemAgentFloat).ConfigureAwait(false);
            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                TransactionType = TransactionType.AgentFloatTopup,
                Timestamp = DateTime.UtcNow,
                DestAccount = agentSubject.ToAccountId(),
                SourceAccount = systemSubject.ToAccountId(),
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "Get", controller: "transactions", values: new { id }));
        }

        [HttpPut("customertopup")]
        public async Task<IActionResult> CustomerTopup(CustomerTopupApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var agentSubject = await GetSubjectAsync(model.AgentNumber, AccountType.AgentFloat).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.CustomerTopup,
                DestAccount = customerSubject.ToAccountId(),
                SourceAccount = agentSubject.ToAccountId(),
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "Get", controller: "transactions", values: new { id }));
        }

        [HttpPut("customersendmoney")]
        public async Task<IActionResult> CustomerSendMoney(CustomerSendMoneyApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var fromSubject = await GetSubjectAsync(model.FromMsisdn, AccountType.CustomerAccount);
            var toSubject = await GetSubjectAsync(model.ToMsisdn, AccountType.CustomerAccount);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);
            var charges = await GetChargesAsync(
                new GetChargeData
                {
                    Amount = model.Amount,
                    Currency = Currency.KES,
                    ChargeToSystemAccount = AccountType.SendMoneyCharge,
                    TransactionType = TransactionType.CustomerSendMoney
                }).ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                TransactionType = TransactionType.CustomerSendMoney,
                Timestamp = DateTime.UtcNow,
                DestAccount = toSubject.ToAccountId(),
                SourceAccount = fromSubject.ToAccountId(),
                Currency = Currency.KES,
                Charges = charges
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "GET", controller: "transactions", values: new { id }));
        }

        [HttpPut("customerwithdrawal")]
        public async Task<IActionResult> CustomerWithdrawal(CustomerWithdrawalApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var agentSubject = await GetSubjectAsync(model.AgentNumber, AccountType.AgentFloat).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);

            var charges = await GetChargesAsync(
                new GetChargeData
                {
                    TransactionType = TransactionType.CustomerSendMoney,
                    Amount = model.Amount,
                    Currency = Currency.KES,
                    ChargeToSystemAccount = AccountType.CustomerWithdrawalCharge,
                    AgentNumber = model.AgentNumber
                }).ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.CustomerWithdrawal,
                DestAccount = agentSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                Charges = charges,
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("paybill")]
        public async Task<IActionResult> PayToCashCollection(PayBillApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var billSubject = await GetSubjectAsync(model.PayBillNumber, AccountType.CashCollectionAccount).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);
            var charges = await GetChargesAsync(new GetChargeData()
            {
                Amount = model.Amount.Value,
                ChargeToSystemAccount = AccountType.CashCollectionCharge,
                Currency = Currency.KES,
                TransactionType = TransactionType.ServicePayment
            }).ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount.Value,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.ServicePayment,
                DestAccount = billSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                ExternalReference = model.AccountNumber,
                Charges = charges,
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("pay2till")]
        public async Task<IActionResult> Pay4Service(TillPaymentApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var tillSubject = await GetSubjectAsync(model.TillNumber, AccountType.TillAccount).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);
            var charges = await GetChargesAsync(new GetChargeData()
            {
                Amount = model.Amount,
                ChargeToSystemAccount = AccountType.ServicePaymentCharge,
                Currency = Currency.KES,
                TransactionType = TransactionType.ServicePayment
            }).ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.PaymentToTill,
                DestAccount = tillSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                Charges = charges,
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [NonAction]
        public static Guid ToGuid(long transactionId)
        {
            Span<byte> guidBytes = stackalloc byte[16];
            //copy ticks
            CopyBytes(guidBytes, transactionId);
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

        [NonAction]
        private async Task<long> NextTxIdAsync()
            => await _bookKeepingContext.NextTransactionIdAsync(HttpContext.RequestAborted).ConfigureAwait(false);

        [NonAction]
        private async Task<CacheableAccountSubject> GetSubjectAsync(string accountNumber, AccountType accountType)
            => await _subjectCache.GetSubjectAsync(accountNumber, accountType, HttpContext.RequestAborted).ConfigureAwait(false);

        [NonAction]
        private async Task<List<Charge>> GetChargesAsync(GetChargeData getChargeData)
        {
            var chargeDatas = await GetChargeDataAsync(
                getChargeData.TransactionType,
                getChargeData.Currency,
                getChargeData.Amount
                ).ConfigureAwait(false);
            if (chargeDatas.Count == 0)
            {
                return new List<Charge>();
            }
            //validate charges
            ChargeDataUtil.Validate(getChargeData.TransactionType, getChargeData.Currency, chargeDatas);
            
            var charges = new List<Charge>(chargeDatas.Count);
            foreach(var chargeData in chargeDatas)
            {
                var id = await NextTxIdAsync().ConfigureAwait(false);
                var accountId = await GetAccountId(chargeData, getChargeData).ConfigureAwait(false);
                charges.Add(new Charge
                {
                    Id = id,
                    Amount = chargeData.ChargeAmount,
                    Currency = getChargeData.Currency,
                    TransactionType = ChargeDataUtil.ChargeTypeToTransactionType(chargeData.ChargeType),
                    PayToAccount = accountId
                });
            }

            return charges;

            async Task<AccountId> GetAccountId(ChargeData chargeData, GetChargeData getChargeData)
            {
                AccountId accountId = null;
                if (chargeData.ChargeType == ChargeType.AgentFees)
                {
                    var info = await _subjectCache.GetSubjectAsync(
                        getChargeData.AgentNumber,
                        AccountType.AgentFeeAccount,
                        HttpContext.RequestAborted
                        ).ConfigureAwait(false);
                    accountId = info.ToAccountId();
                }
                else if (chargeData.ChargeType == ChargeType.SystemCharge)
                {
                    var info = await _subjectCache.GetSubjectAsync(
                        SystemAcc,
                        getChargeData.ChargeToSystemAccount.Value,
                        HttpContext.RequestAborted
                        ).ConfigureAwait(false);
                    accountId = info.ToAccountId();
                }
                else
                {
                    throw new ArgumentException($"Unexpected charge type {chargeData.ChargeType}");
                }
                return accountId;
            }
        }

        [NonAction]
        private async Task<List<ChargeData>> GetChargeDataAsync(TransactionType transactionType, Currency currency, decimal amount)
        {
            var now = DateTime.UtcNow;
            var charges = await _bookKeepingContext.ChargeData
                .Where(c => c.TransactionType == transactionType && c.Currency == currency && c.ChargeAmount > 0)
                .Where(c => c.FromDate <= now && c.ToDate == null && c.MinAmount <= amount && c.MaxAmount >= amount)
                .ToListAsync(HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return charges;
        }

        [NonAction]
        private static List<T> ListOf<T>(params T[] values)
            => values == null ? new List<T>() : new List<T>(values);

        private record GetChargeData
        {
            public TransactionType TransactionType { get; init; }
            public decimal Amount { get; init; }
            public Currency Currency { get; init; }
            public AccountType? ChargeToSystemAccount { get; init; }
            public string AgentNumber { get; init; }
        }
    }
}
