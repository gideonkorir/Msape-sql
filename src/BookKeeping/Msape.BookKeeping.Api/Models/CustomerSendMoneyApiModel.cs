using FluentValidation;
using Msape.BookKeeping.Api.Infra;

namespace Msape.BookKeeping.Api.Models
{
    public class CustomerSendMoneyApiModel
    {
        public string FromMsisdn { get; init; }
        public string ToMsisdn { get; init; }
        public decimal Amount { get; init; }
    }

    public class CustomerSendMoneyApiModelValidator : AbstractValidator<CustomerSendMoneyApiModel>
    {
        public CustomerSendMoneyApiModelValidator(ICosmosAccount cosmosAccount)
        {
            RuleFor(c => c.FromMsisdn).NotEmpty()
                .WithMessage("The source customer number is required")
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
                    return subject != null && subject.AccountType == Data.AccountType.AgentFloat;
                })
                .WithMessage(context => $"Customer account with number {context.FromMsisdn} was not found"); ;
            RuleFor(c => c.ToMsisdn).NotEmpty()
                .WithMessage("The destination customer number is required")
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
                .WithMessage(context => $"Customer account with number {context.ToMsisdn} was not found"); ;
            RuleFor(c => c.Amount).InclusiveBetween(50, 300000)
                .WithMessage("The amount must range between 50 and 300000");
        }
    }
}
