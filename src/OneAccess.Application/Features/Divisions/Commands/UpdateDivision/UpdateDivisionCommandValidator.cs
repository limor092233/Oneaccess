using FluentValidation;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Divisions.Commands.UpdateDivision;

public class UpdateDivisionCommandValidator : AbstractValidator<UpdateDivisionCommand>
{
    public UpdateDivisionCommandValidator(IReadDbContext readDbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Division ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Division name is required.")
            .MaximumLength(100).WithMessage("Division name must not exceed 100 characters.")
            .Must((command, name) =>
            {
                var lower = name.Trim().ToLower();
                return !readDbContext.Divisions.Any(d => d.Id != command.Id && d.Name.ToLower() == lower);
            })
            .WithMessage("Another division with this name already exists.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
