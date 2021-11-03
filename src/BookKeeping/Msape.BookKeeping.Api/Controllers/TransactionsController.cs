using Microsoft.AspNetCore.Mvc;
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
            var chargeSubject = await GetSubjectAsync(SystemAcc, AccountType.SendMoneyCharge);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);
            var chargeId = await NextTxIdAsync().ConfigureAwait(false);

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
                Charges = ListOf(
                    new Charge()
                    {
                        Id = chargeId,
                        Amount = 30,
                        Currency = 0,
                        TransactionType = TransactionType.TransactionCharge,
                        PayToAccount = chargeSubject.ToAccountId()
                    }
                    )
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
            var chargeSubject = await GetSubjectAsync(SystemAcc, AccountType.CustomerWithdrawalCharge).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);
            var chargeId = await NextTxIdAsync().ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.CustomerWithdrawal,
                DestAccount = agentSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                Charges = ListOf(
                    new Charge()
                    {
                        Id = chargeId,
                        Amount = Math.Ceiling(model.Amount * 0.01M),
                        Currency = 0,
                        TransactionType = TransactionType.TransactionCharge,
                        PayToAccount = chargeSubject.ToAccountId()
                    }
                    ),
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("paybill")]
        public async Task<IActionResult> PayToMoneyCollectionAccount(PayBillApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var billSubject = await GetSubjectAsync(model.PayBillNumber, AccountType.CashCollectionAccount).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount.Value,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.BillPayment,
                DestAccount = billSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                ExternalReference = model.AccountNumber,
                Charges = null,
                Currency = Currency.KES
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("pay2till")]
        public async Task<IActionResult> Pay2Till(TillPaymentApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var tillSubject = await GetSubjectAsync(model.TillNumber, AccountType.TillAccount).ConfigureAwait(false);
            var customerSubject = await GetSubjectAsync(model.CustomerNumber, AccountType.CustomerAccount).ConfigureAwait(false);

            //send message to queue
            var id = await NextTxIdAsync().ConfigureAwait(false);

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                PostingId = ToGuid(id),
                TransactionId = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.PaymentToTill,
                DestAccount = tillSubject.ToAccountId(),
                SourceAccount = customerSubject.ToAccountId(),
                Charges = null,
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

        [NonAction]
        private async Task<long> NextTxIdAsync()
            => await _bookKeepingContext.NextTransactionIdAsync(HttpContext.RequestAborted).ConfigureAwait(false);

        [NonAction]
        private async Task<CacheableAccountSubject> GetSubjectAsync(string accountNumber, AccountType accountType)
            => await _subjectCache.GetSubjectAsync(accountNumber, accountType, HttpContext.RequestAborted).ConfigureAwait(false);

        [NonAction]
        private async Task<List<ChargeData>> GetChargeDataAsync(TransactionType transactionType, Money amount)
        {
            var now = DateTime.UtcNow;
            var charges = await _bookKeepingContext.ChargeConfigurations
                .Where(c => c.TransactionType == transactionType && c.Currency == amount.Currency)
                .SelectMany(c => c.Data)
                .Where(c => c.FromDate >= now && c.ToDate != null && c.MinAmount <= amount.Value && c.MaxAmount >= amount.Value)
                .Where(c => c.ChargeAmount > 0M)
                .ToListAsync(HttpContext.RequestAborted)
                .ConfigureAwait(false);
            return charges;
        }

        [NonAction]
        private static List<T> ListOf<T>(params T[] values)
            => values == null ? new List<T>() : new List<T>(values);
    }
}
