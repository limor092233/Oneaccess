using FluentValidation;

namespace OneAccess.Application.Features.Setup.Commands.InitializeSystemAdmin;

public class InitializeSystemAdminCommandValidator : AbstractValidator<InitializeSystemAdminCommand>
{
    public InitializeSystemAdminCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Setup code is required.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");
    }
}
