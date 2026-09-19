using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.AssignUserSubSystemAccess;

/// <summary>
/// Command to add a sub-system to a user's per-user override allow-list.
/// </summary>
public record AssignUserSubSystemAccessCommand(Guid UserId, Guid SubSystemId) : IRequest<Result>;
