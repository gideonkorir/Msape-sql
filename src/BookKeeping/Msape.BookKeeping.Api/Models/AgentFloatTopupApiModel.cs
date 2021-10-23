using FluentValidation;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data.EF;

namespace Msape.BookKeeping.Api.Models
{
    public record AgentFloatTopupApiModel
    {
        public string AgentNumber { get; init; }
        public decimal Amount { get; init; }
    }

    public class AgentFloatTopupApiModelValidator : AbstractValidator<AgentFloatTopupApiModel>
    {
        public AgentFloatTopupApiModelValidator(ISubjectCache subjectCache)
        {
            RuleFor(c => c.AgentNumber).NotEmpty()
                .WithMessage("The agent number is required")
                .MustAsync(async (context, agentNumber, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(agentNumber, Data.AccountType.AgentFloat, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"Agent float account with number {context.AgentNumber} was not found");
            RuleFor(c => c.Amount).InclusiveBetween(100, 10_000_000)
                .WithMessage("The topup amount must be between 100 and 10 million");
            
        }
    }
}
