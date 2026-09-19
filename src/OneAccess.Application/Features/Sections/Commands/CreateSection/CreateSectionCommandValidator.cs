using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Sections.Commands.CreateSection;

public class CreateSectionCommandValidator : AbstractValidator<CreateSectionCommand>
{
    public CreateSectionCommandValidator(IReadDbContext readDbContext)
    {
        RuleFor(x => x.DivisionId)
            .NotEmpty().WithMessage("Division ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Section name is required.")
            .MaximumLength(100).WithMessage("Section name must not exceed 100 characters.")
            .MustAsync(async (command, name, ct) =>
            {
                var lower = name.Trim().ToLower();
                return !await readDbContext.Sections.AnyAsync(s => s.DivisionId == command.DivisionId && s.Name.ToLower() == lower, ct);
            })
            .WithMessage("A section with this name already exists in this division.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
