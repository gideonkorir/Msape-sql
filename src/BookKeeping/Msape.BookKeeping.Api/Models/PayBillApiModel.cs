using FluentValidation;
using Msape.BookKeeping.Data;

namespace Msape.BookKeeping.Api.Models
{
    public record PayBillApiModel(string CustomerNumber, string PayBillNumber, string AccountNumber, Money Amount);

    public class PayBillApiModelValidator : AbstractValidator<PayBillApiModel>
    {
        public PayBillApiModelValidator()
        {
            RuleFor(c => c.CustomerNumber)
                .NotEmpty()
                .WithMessage("The customer number is required");
            RuleFor(c => c.PayBillNumber)
                .NotEmpty()
                .WithMessage("The pay bill number is required");
            RuleFor(c => c.AccountNumber)
                .NotEmpty()
                .WithMessage("The account number is required");
            RuleFor(c => c.Amount)
                .NotEmpty()
                .WithMessage("The amount is required");
        }
    }
}
