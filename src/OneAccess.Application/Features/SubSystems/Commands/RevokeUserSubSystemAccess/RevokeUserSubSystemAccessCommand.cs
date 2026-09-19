using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.RevokeUserSubSystemAccess;

/// <summary>
/// Command to remove a sub-system from a user's override allow-list.
/// </summary>
public record RevokeUserSubSystemAccessCommand(Guid UserId, Guid SubSystemId) : IRequest<Result>;
