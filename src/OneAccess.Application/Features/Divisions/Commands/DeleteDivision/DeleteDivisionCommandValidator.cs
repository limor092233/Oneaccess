using FluentValidation;

namespace OneAccess.Application.Features.Divisions.Commands.DeleteDivision;

public class DeleteDivisionCommandValidator : AbstractValidator<DeleteDivisionCommand>
{
    public DeleteDivisionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Division ID is required.");
    }
}
