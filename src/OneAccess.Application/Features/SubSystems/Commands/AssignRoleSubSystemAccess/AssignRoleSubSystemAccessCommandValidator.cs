using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.AssignRoleSubSystemAccess;

/// <summary>
/// Validator for AssignRoleSubSystemAccessCommand.
/// </summary>
public class AssignRoleSubSystemAccessCommandValidator : AbstractValidator<AssignRoleSubSystemAccessCommand>
{
    public AssignRoleSubSystemAccessCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required.");

        RuleFor(x => x.SubSystemId)
            .NotEmpty().WithMessage("SubSystemId is required.");
    }
}
