using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Validator for CreateUserCommand enforcing formatting, length, and Division/Section consistency.
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        // Section without Division is invalid
        RuleFor(x => x.SectionId)
            .Must((cmd, sectionId) => !sectionId.HasValue || cmd.DivisionId.HasValue)
            .WithMessage("DivisionId must be specified when SectionId is provided.");

        // Section must belong to Division
        RuleFor(x => x.SectionId)
            .MustAsync(async (cmd, sectionId, ct) =>
            {
                if (!sectionId.HasValue || !cmd.DivisionId.HasValue)
                {
                    return true;
                }

                var section = await db.Sections.FirstOrDefaultAsync(s => s.Id == sectionId.Value, ct);
                return section != null && section.DivisionId == cmd.DivisionId.Value;
            })
            .WithMessage("Section does not belong to the specified Division.");
    }
}
