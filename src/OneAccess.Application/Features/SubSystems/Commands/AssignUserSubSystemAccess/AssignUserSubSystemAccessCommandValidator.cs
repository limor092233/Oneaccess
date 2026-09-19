using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;

/// <summary>
/// Validator for AssignUserSubSystemAccessCommand.
/// </summary>
public class AssignUserSubSystemAccessCommandValidator : AbstractValidator<AssignUserSubSystemAccessCommand>
{
    public AssignUserSubSystemAccessCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.SubSystemId)
            .NotEmpty().WithMessage("SubSystemId is required.");
    }
}
