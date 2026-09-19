using FluentValidation;

namespace OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;

public class AssignUserDivisionCommandValidator : AbstractValidator<AssignUserDivisionCommand>
{
    public AssignUserDivisionCommandValidator()
    {
        RuleFor(x => x.DivisionId)
            .NotEmpty().WithMessage("Division ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
