using FluentValidation;

namespace OneAccess.Application.Features.RolePermissions.Commands.RevokePermission;

/// <summary>
/// Validator for RevokePermissionCommand.
/// </summary>
public class RevokePermissionCommandValidator : AbstractValidator<RevokePermissionCommand>
{
    public RevokePermissionCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required.");
    }
}
