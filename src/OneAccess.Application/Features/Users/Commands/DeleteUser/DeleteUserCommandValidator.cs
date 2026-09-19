using FluentValidation;

namespace OneAccess.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Validator for DeleteUserCommand.
/// </summary>
public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
