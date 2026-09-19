using FluentValidation;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Divisions.Commands.CreateDivision;

public class CreateDivisionCommandValidator : AbstractValidator<CreateDivisionCommand>
{
    public CreateDivisionCommandValidator(IReadDbContext readDbContext)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Division name is required.")
            .MaximumLength(100).WithMessage("Division name must not exceed 100 characters.")
            .Must((name) =>
            {
                var lower = name.Trim().ToLower();
                return !readDbContext.Divisions.Any(d => d.Name.ToLower() == lower);
            })
            .WithMessage("A division with this name already exists.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
