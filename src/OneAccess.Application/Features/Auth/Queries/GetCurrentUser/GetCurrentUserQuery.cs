using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query retrieving the currently authenticated user's profile, roles, permissions, and effective sub-systems.
/// </summary>
public record GetCurrentUserQuery : IRequest<Result<CurrentUserResponse>>;
