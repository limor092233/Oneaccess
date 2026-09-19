using FluentValidation;

namespace OneAccess.Application.Features.Divisions.Commands.RevokeUserDivision;

public class RevokeUserDivisionCommandValidator : AbstractValidator<RevokeUserDivisionCommand>
{
    public RevokeUserDivisionCommandValidator()
    {
        RuleFor(x => x.DivisionId)
            .NotEmpty().WithMessage("Division ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
