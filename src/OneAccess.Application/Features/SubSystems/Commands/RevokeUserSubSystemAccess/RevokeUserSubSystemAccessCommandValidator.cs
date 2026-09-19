using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeUserSubSystemAccess;

/// <summary>
/// Validator for RevokeUserSubSystemAccessCommand.
/// </summary>
public class RevokeUserSubSystemAccessCommandValidator : AbstractValidator<RevokeUserSubSystemAccessCommand>
{
    public RevokeUserSubSystemAccessCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.SubSystemId)
            .NotEmpty().WithMessage("SubSystemId is required.");
    }
}
