using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Command to update an existing role.
/// </summary>
public record UpdateRoleCommand(Guid Id, string Name, string Description) : IRequest<Result<UpdateRoleResponse>>;
