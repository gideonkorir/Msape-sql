using FluentValidation;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data;

namespace Msape.BookKeeping.Api.Models
{
    public record PayBillApiModel(string CustomerNumber, string PayBillNumber, string AccountNumber, Money Amount);

    public class PayBillApiModelValidator : AbstractValidator<PayBillApiModel>
    {
        public PayBillApiModelValidator(ISubjectCache subjectCache)
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
            RuleFor(c => c.PayBillNumber)
                .NotEmpty()
                .WithMessage("The pay bill number is required")
                .MustAsync(async (context, paybill, cancellationToken) =>
                {
                    var subject = await subjectCache.GetSubjectAsync(paybill, Data.AccountType.CashCollectionAccount, cancellationToken)
                        .ConfigureAwait(false);
                    return subject != null;
                })
                .WithMessage(context => $"PayBill account with number {context.PayBillNumber} was not found"); ;
            RuleFor(c => c.AccountNumber)
                .NotEmpty()
                .WithMessage("The account number is required");
            RuleFor(c => c.Amount)
                .NotEmpty()
                .WithMessage("The amount is required");
        }
    }
}
