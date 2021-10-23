using FluentValidation;
using Msape.BookKeeping.Api.Infra;

namespace Msape.BookKeeping.Api.Models
{
    public record CustomerWithdrawalApiModel(string CustomerNumber, string AgentNumber, decimal Amount);

    public class CustomerWithdrawalApiModelValidator : AbstractValidator<CustomerWithdrawalApiModel>
    {
        public CustomerWithdrawalApiModelValidator(ISubjectCache subjectCache)
        {
            RuleFor(c => c.AgentNumber).NotEmpty()
                .WithMessage("The agent number is required")
                .MustAsync(async (context, agentNumber, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(agentNumber, Data.AccountType.AgentFloat, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"Agent float account with number {context.AgentNumber} was not found"); ;
            RuleFor(c => c.CustomerNumber).NotEmpty()
                .WithMessage("The customer number is required")
                .MustAsync(async (context, customerNumber, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(customerNumber, Data.AccountType.CustomerAccount, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"Customer account with number {context.CustomerNumber} was not found"); ;
            RuleFor(c => c.Amount).InclusiveBetween(100, 70_000)
                .WithMessage("The withdrawal amount must range between 10 and 70,000");
        }
    }
}
