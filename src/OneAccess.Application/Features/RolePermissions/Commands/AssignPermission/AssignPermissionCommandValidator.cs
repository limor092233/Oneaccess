using FluentValidation;

namespace OneAccess.Application.Features.RolePermissions.Commands.AssignPermission;

/// <summary>
/// Validator for AssignPermissionCommand.
/// </summary>
public class AssignPermissionCommandValidator : AbstractValidator<AssignPermissionCommand>
{
    public AssignPermissionCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required.");
    }
}
