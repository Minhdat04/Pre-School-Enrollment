using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(100);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])")
                .WithMessage("Password must contain uppercase, lowercase, number, and special character");

            // ... more rules
        }
    }
}
