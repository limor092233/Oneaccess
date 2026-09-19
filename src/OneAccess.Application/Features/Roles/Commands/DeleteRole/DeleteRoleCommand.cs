using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Command to delete an existing custom role.
/// </summary>
public record DeleteRoleCommand(Guid Id) : IRequest<Result>;
