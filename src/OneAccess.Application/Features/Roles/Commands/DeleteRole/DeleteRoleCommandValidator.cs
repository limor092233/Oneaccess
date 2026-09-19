using FluentValidation;

namespace OneAccess.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Validator for DeleteRoleCommand.
/// </summary>
public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}
