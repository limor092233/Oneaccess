using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Roles.Queries.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IReadDbContext _readDbContext;

    public GetRolesQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = _readDbContext.Roles
            .OrderBy(r => r.Name)
            .ToList();

        var rolePermissions = (from rp in _readDbContext.RolePermissions
                               join p in _readDbContext.Permissions on rp.PermissionId equals p.Id
                               select new { rp.RoleId, p.Code })
                               .ToList();

        var roleDtoList = roles.Select(r =>
        {
            var perms = rolePermissions
                .Where(rp => rp.RoleId == r.Id)
                .Select(rp => rp.Code)
                .OrderBy(c => c)
                .ToList();

            return new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                perms,
                r.CreatedAt);
        }).ToList();

        return Task.FromResult(Result<IReadOnlyList<RoleDto>>.Success(roleDtoList));
    }
}
