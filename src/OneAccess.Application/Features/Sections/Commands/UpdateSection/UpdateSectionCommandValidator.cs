using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Sections.Commands.UpdateSection;

public class UpdateSectionCommandValidator : AbstractValidator<UpdateSectionCommand>
{
    public UpdateSectionCommandValidator(IReadDbContext readDbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Section ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Section name is required.")
            .MaximumLength(100).WithMessage("Section name must not exceed 100 characters.")
            .MustAsync(async (command, name, ct) =>
            {
                var lower = name.Trim().ToLower();
                var currentSection = await readDbContext.Sections.FirstOrDefaultAsync(s => s.Id == command.Id, ct);
                if (currentSection == null)
                {
                    return true;
                }

                return !await readDbContext.Sections.AnyAsync(s => s.Id != command.Id && s.DivisionId == currentSection.DivisionId && s.Name.ToLower() == lower, ct);
            })
            .WithMessage("Another section with this name already exists in this division.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
