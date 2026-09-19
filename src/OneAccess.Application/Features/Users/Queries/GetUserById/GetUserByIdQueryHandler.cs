using MediatR;
using OneAccess.Application.Common.Interfaces;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailsDto>>
{
    private readonly IReadDbContext _readDbContext;

    public GetUserByIdQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public Task<Result<UserDetailsDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = _readDbContext.Users
            .FirstOrDefault(u => u.Id == request.Id);

        if (user == null)
        {
            return Task.FromResult(Result<UserDetailsDto>.NotFound("User not found."));
        }

        var roles = (from ur in _readDbContext.UserRoles
                     join r in _readDbContext.Roles on ur.RoleId equals r.Id
                     where ur.UserId == user.Id
                     select r.Name)
                     .ToList();

        var dto = new UserDetailsDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Status,
            user.IsSystemAdministrator,
            user.DivisionId,
            user.SectionId,
            roles,
            user.CreatedAt,
            user.UpdatedAt);

        return Task.FromResult(Result<UserDetailsDto>.Success(dto));
    }
}
