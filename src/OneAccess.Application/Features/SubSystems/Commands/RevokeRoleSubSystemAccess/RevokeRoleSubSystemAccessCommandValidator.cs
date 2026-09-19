using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeRoleSubSystemAccess;

/// <summary>
/// Validator for RevokeRoleSubSystemAccessCommand.
/// </summary>
public class RevokeRoleSubSystemAccessCommandValidator : AbstractValidator<RevokeRoleSubSystemAccessCommand>
{
    public RevokeRoleSubSystemAccessCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required.");

        RuleFor(x => x.SubSystemId)
            .NotEmpty().WithMessage("SubSystemId is required.");
    }
}
