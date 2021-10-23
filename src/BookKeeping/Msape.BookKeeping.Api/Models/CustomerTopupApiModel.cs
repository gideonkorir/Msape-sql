using FluentValidation;
using Msape.BookKeeping.Api.Infra;

namespace Msape.BookKeeping.Api.Models
{
    public record CustomerTopupApiModel
    {
        public string AgentNumber { get; init; }
        public string CustomerNumber { get; init; }
        public decimal Amount { get; init; }
    }

    public class CustomerTopupApiModelValidator : AbstractValidator<CustomerTopupApiModel>
    {
        public CustomerTopupApiModelValidator(ICosmosAccount cosmosAccount)
        {
            RuleFor(c => c.AgentNumber).NotEmpty()
                .WithMessage("The agent number is required")
                .MustAsync(async (context, agentNumber, cancellationToken) =>
                {
                    var subject = await AccountNumberQueryHelper.GetSubject(
                        container: cosmosAccount.AccountNumbers,
                        linkedAccountKey: "AGENT_FLOAT",
                        accountNumber: agentNumber,
                        partitionKeyIsReversedAccountNumber: true,
                        requestOptions: null,
                        cancellationToken: cancellationToken
                        )
                        .ConfigureAwait(false);
                    return subject != null && subject.AccountType == Data.AccountType.AgentFloat;
                })
                .WithMessage(context => $"Agent float account with number {context.AgentNumber} was not found"); ;
            RuleFor(c => c.CustomerNumber).NotEmpty()
                .WithMessage("The customer number is required")
                .MustAsync(async (context, customerNumber, cancellationToken) =>
                {
                    var subject = await AccountNumberQueryHelper.GetSubject(
                        container: cosmosAccount.AccountNumbers,
                        linkedAccountKey: "CUSTOMER_ACCOUNT",
                        accountNumber: customerNumber,
                        partitionKeyIsReversedAccountNumber: true,
                        requestOptions: null,
                        cancellationToken: cancellationToken
                        )
                        .ConfigureAwait(false);
                    return subject != null && subject.AccountType == Data.AccountType.CustomerAccount;
                })
                .WithMessage(context => $"Customer account with number {context.CustomerNumber} was not found"); ;
            RuleFor(c => c.Amount).InclusiveBetween(50, 300000)
                .WithMessage("The amount must range between 50 and 300000");
        }
    }
}
