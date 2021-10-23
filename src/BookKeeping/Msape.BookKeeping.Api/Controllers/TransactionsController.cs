using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Api.Models;
using Msape.BookKeeping.Data;
using Msape.BookKeeping.InternalContracts;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    public partial class TransactionsController : ControllerBase
    {
        private readonly ICosmosAccount _cosmosAccount;

        public TransactionsController(ICosmosAccount cosmosAccount)
        {
            _cosmosAccount = cosmosAccount ?? throw new ArgumentNullException(nameof(cosmosAccount));
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get([FromRoute] string id)
        {
            try
            {
                var response = await _cosmosAccount.Transactions.ReadItemAsync<Transaction>(
                    id: id,
                    partitionKey: new PartitionKey(id),
                    requestOptions: new ItemRequestOptions()
                    {

                    },
                    cancellationToken: HttpContext.RequestAborted
                    ).ConfigureAwait(false);
                return Ok(TransactionApiModel.Create(response.Resource));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new
                {
                    ErrorMessage = $"Transaction with id {id} was not found"
                });
            }
        }

        [HttpPut("agentfloattopup")]
        public async Task<IActionResult> AgentFloatTopup(AgentFloatTopupApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var subject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "AGENT_FLOAT",
                accountNumber: model.AgentNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            var systemSubject = await SystemAccountNumberHelper.GetAgentFloatDebitAccount(
                container: _cosmosAccount.AccountNumbers,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                Id = id,
                TransactionType = TransactionType.AgentFloatTopup,
                Timestamp = DateTime.UtcNow,
                CreditAccountId = new AccountId()
                {
                    Id = subject.Account.Id,
                    PartitionKey = subject.Account.PartitionKey,
                    Name = subject.Name,
                    AccountNumber = subject.AccountNumber,
                    AccountType = subject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = systemSubject.Account.Id,
                    PartitionKey = systemSubject.Account.PartitionKey,
                    Name = systemSubject.Name,
                    AccountNumber = systemSubject.AccountNumber,
                    AccountType = systemSubject.AccountType
                },
                Currency = 0
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "Get", controller: "transactions", values: new { id }));
        }

        [HttpPut("customertopup")]
        public async Task<IActionResult> CustomerTopup(CustomerTopupApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var agentSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "AGENT_FLOAT",
                accountNumber: model.AgentNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var customerSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.CustomerNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                Id = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.CustomerTopup,
                CreditAccountId = new AccountId()
                {
                    Id = customerSubject.Account.Id,
                    PartitionKey = customerSubject.Account.PartitionKey,
                    Name = customerSubject.Name,
                    AccountNumber = customerSubject.AccountNumber,
                    AccountType = customerSubject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = agentSubject.Account.Id,
                    PartitionKey = agentSubject.Account.PartitionKey,
                    Name = agentSubject.Name,
                    AccountNumber = agentSubject.AccountNumber,
                    AccountType = agentSubject.AccountType
                },
                Currency = 0
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "Get", controller: "transactions", values: new { id }));
        }

        [HttpPut("customersendmoney")]
        public async Task<IActionResult> CustomerSendMoney(CustomerSendMoneyApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var fromSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.FromMsisdn,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var toSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.ToMsisdn,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var chargeSubject = await SystemAccountNumberHelper.GetCustomerSendMoneyChargeCreditAccount(
                container: _cosmosAccount.AccountNumbers,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                Id = id,
                TransactionType = TransactionType.CustomerSendMoney,
                Timestamp = DateTime.UtcNow,
                CreditAccountId = new AccountId()
                {
                    Id = toSubject.Account.Id,
                    PartitionKey = toSubject.Account.PartitionKey,
                    Name = toSubject.Name,
                    AccountNumber = toSubject.AccountNumber,
                    AccountType = toSubject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = fromSubject.Account.Id,
                    PartitionKey = fromSubject.Account.PartitionKey,
                    Name = fromSubject.Name,
                    AccountNumber = fromSubject.AccountNumber,
                    AccountType = fromSubject.AccountType
                },
                Currency = 0,
                Charge = new Charge()
                {
                    Id = NewId.NextGuid(),
                    Amount = 30,
                    Currency = 0,
                    TransactionType = TransactionType.SendMoneyCharge,
                    PayToAccount = new AccountId()
                    {
                        AccountNumber = chargeSubject.AccountNumber,
                        AccountType = chargeSubject.AccountType,
                        PartitionKey = chargeSubject.Account.PartitionKey,
                        Id = chargeSubject.Account.Id,
                        Name = chargeSubject.Name
                    }
                }
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink(action: "GET", controller: "transactions", values: new { id }));
        }

        [HttpPut("customerwithdrawal")]
        public async Task<IActionResult> CustomerWithdrawal(CustomerWithdrawalApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var agentSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "AGENT_FLOAT",
                accountNumber: model.AgentNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var customerSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.CustomerNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var chargeSubject = await SystemAccountNumberHelper.GetCustomerWithdrawalChargeCreditAccount(
                container: _cosmosAccount.AccountNumbers,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                Id = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.CustomerWithdrawal,
                CreditAccountId = new AccountId()
                {
                    Id = agentSubject.Account.Id,
                    PartitionKey = agentSubject.Account.PartitionKey,
                    Name = agentSubject.Name,
                    AccountNumber = agentSubject.AccountNumber,
                    AccountType = agentSubject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = customerSubject.Account.Id,
                    PartitionKey = customerSubject.Account.PartitionKey,
                    Name = customerSubject.Name,
                    AccountNumber = customerSubject.AccountNumber,
                    AccountType = customerSubject.AccountType
                },
                Charge = new Charge()
                {
                    Id = NewId.NextGuid(),
                    Amount = Math.Ceiling(model.Amount * 0.01M),
                    Currency = 0,
                    TransactionType = TransactionType.CustomerWithdrawalCharge,
                    PayToAccount = new AccountId()
                    {
                        AccountNumber = chargeSubject.AccountNumber,
                        AccountType = chargeSubject.AccountType,
                        PartitionKey = chargeSubject.Account.PartitionKey,
                        Id = chargeSubject.Account.Id,
                        Name = chargeSubject.Name
                    }
                },
                Currency = 0
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("paybill")]
        public async Task<IActionResult> Pay2Till(PayBillApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var tillSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "PAYBILL_NUMBER",
                accountNumber: model.PayBillNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var customerSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.CustomerNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount.Value,
                Id = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.BillPayment,
                CreditAccountId = new AccountId()
                {
                    Id = tillSubject.Account.Id,
                    PartitionKey = tillSubject.Account.PartitionKey,
                    Name = tillSubject.Name,
                    AccountNumber = tillSubject.AccountNumber,
                    AccountType = tillSubject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = customerSubject.Account.Id,
                    PartitionKey = customerSubject.Account.PartitionKey,
                    Name = customerSubject.Name,
                    AccountNumber = customerSubject.AccountNumber,
                    AccountType = customerSubject.AccountType
                },
                ExternalReference = model.AccountNumber,
                Charge = null,
                Currency = 0
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }

        [HttpPut("pay2till")]
        public async Task<IActionResult> Pay2Till(TillPaymentApiModel model, [FromServices] ISendTransactionCommand commandSender)
        {
            var tillSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "TILL_NUMBER",
                accountNumber: model.TillNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);
            var customerSubject = await AccountNumberQueryHelper.GetSubject(
                container: _cosmosAccount.AccountNumbers,
                linkedAccountKey: "CUSTOMER_ACCOUNT",
                accountNumber: model.CustomerNumber,
                partitionKeyIsReversedAccountNumber: true,
                requestOptions: null,
                cancellationToken: HttpContext.RequestAborted
                )
                .ConfigureAwait(false);

            //send message to queue
            var id = NewId.NextGuid();

            await commandSender.Send(new PostTransaction()
            {
                Amount = model.Amount,
                Id = id,
                Timestamp = DateTime.UtcNow,
                TransactionType = TransactionType.PaymentToTill,
                CreditAccountId = new AccountId()
                {
                    Id = tillSubject.Account.Id,
                    PartitionKey = tillSubject.Account.PartitionKey,
                    Name = tillSubject.Name,
                    AccountNumber = tillSubject.AccountNumber,
                    AccountType = tillSubject.AccountType
                },
                DebitAccountId = new AccountId()
                {
                    Id = customerSubject.Account.Id,
                    PartitionKey = customerSubject.Account.PartitionKey,
                    Name = customerSubject.Name,
                    AccountNumber = customerSubject.AccountNumber,
                    AccountType = customerSubject.AccountType
                },
                Charge = null,
                Currency = 0
            },
            HttpContext.RequestAborted
            ).ConfigureAwait(false);

            return Accepted(Url.ActionLink("Get", "transactions", values: new { id }));
        }
    }
}
