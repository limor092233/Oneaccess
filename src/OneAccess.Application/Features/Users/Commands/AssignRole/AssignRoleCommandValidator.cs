using FluentValidation;

namespace OneAccess.Application.Features.Users.Commands.AssignRole;

/// <summary>
/// Validator for AssignRoleCommand.
/// </summary>
public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}
