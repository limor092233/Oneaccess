using FluentValidation;

namespace OneAccess.Application.Features.SubSystems.Queries.GetRoleSubSystems;

/// <summary>
/// Validator for GetRoleSubSystemsQuery.
/// </summary>
public class GetRoleSubSystemsQueryValidator : AbstractValidator<GetRoleSubSystemsQuery>
{
    public GetRoleSubSystemsQueryValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("RoleId is required.");
    }
}
