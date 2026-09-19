using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Queries.GetUserSubSystems;

/// <summary>
/// Validator for GetUserSubSystemsQuery.
/// </summary>
public class GetUserSubSystemsQueryValidator : AbstractValidator<GetUserSubSystemsQuery>
{
    public GetUserSubSystemsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
