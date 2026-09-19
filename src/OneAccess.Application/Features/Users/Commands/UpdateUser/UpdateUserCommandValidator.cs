using FluentValidation;
using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Validator for UpdateUserCommand enforcing formatting and Division/Section consistency.
/// </summary>
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        // Section without Division is invalid
        RuleFor(x => x)
            .Must(x => !x.SectionId.HasValue || x.DivisionId.HasValue)
            .WithMessage("DivisionId must be specified when SectionId is provided.");

        // Section must belong to Division
        RuleFor(x => x)
            .Must((cmd) =>
            {
                if (!cmd.SectionId.HasValue || !cmd.DivisionId.HasValue)
                {
                    return true;
                }

                var section = db.Sections.FirstOrDefault(s => s.Id == cmd.SectionId.Value);
                return section != null && section.DivisionId == cmd.DivisionId.Value;
            })
            .WithMessage("Section does not belong to the specified Division.");
    }
}
