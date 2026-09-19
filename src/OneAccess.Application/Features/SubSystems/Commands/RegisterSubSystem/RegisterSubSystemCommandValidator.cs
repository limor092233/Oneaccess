using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;

/// <summary>
/// Validator for RegisterSubSystemCommand.
/// </summary>
public class RegisterSubSystemCommandValidator : AbstractValidator<RegisterSubSystemCommand>
{
    public RegisterSubSystemCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Audience)
            .NotEmpty().WithMessage("Audience is required.")
            .MaximumLength(200).WithMessage("Audience must not exceed 200 characters.");

        RuleFor(x => x.BaseUrl)
            .MaximumLength(500).WithMessage("BaseUrl must not exceed 500 characters.");
    }
}
