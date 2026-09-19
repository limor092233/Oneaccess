using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;

/// <summary>
/// Validator for UpdateSubSystemCommand.
/// </summary>
public class UpdateSubSystemCommandValidator : AbstractValidator<UpdateSubSystemCommand>
{
    public UpdateSubSystemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Sub-system ID is required.");

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
