using FluentValidation;
using Msape.BookKeeping.Api.Infra;

namespace Msape.BookKeeping.Api.Models
{
    public record TillPaymentApiModel(string CustomerNumber, string TillNumber, decimal Amount);

    public class TillPaymentApiModelValidator : AbstractValidator<TillPaymentApiModel>
    {
        public TillPaymentApiModelValidator(ISubjectCache subjectCache)
        {
            RuleFor(c => c.CustomerNumber)
                .NotEmpty()
                .WithMessage("The customer number is required")
                .MustAsync(async (context, customerNumber, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(customerNumber, Data.AccountType.CustomerAccount, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"Customer account with number {context.CustomerNumber} was not found"); ;
            RuleFor(c => c.TillNumber)
                .NotEmpty()
                .WithMessage("The till number is required")
                .MustAsync(async (context, tillNumber, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(tillNumber, Data.AccountType.TillAccount, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"Till account with number {context.TillNumber} was not found"); ;
            RuleFor(c => c.Amount)
                .NotEmpty()
                .WithMessage("The amount is required");
        }
    }
}
