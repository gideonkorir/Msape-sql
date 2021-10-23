using FluentValidation;

namespace Msape.BookKeeping.Api.Models
{
    public record TillPaymentApiModel(string CustomerNumber, string TillNumber, decimal Amount);

    public class TillPaymentApiModelValidator : AbstractValidator<TillPaymentApiModel>
    {
        public TillPaymentApiModelValidator()
        {
            RuleFor(c => c.CustomerNumber)
                .NotEmpty()
                .WithMessage("The customer number is required");
            RuleFor(c => c.TillNumber)
                .NotEmpty()
                .WithMessage("The till number is required");
            RuleFor(c => c.Amount)
                .NotEmpty()
                .WithMessage("The amount is required");
        }
    }
}
