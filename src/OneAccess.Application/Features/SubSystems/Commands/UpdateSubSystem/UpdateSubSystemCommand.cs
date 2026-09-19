using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.UpdateSubSystem;

/// <summary>
/// Command to update an existing sub-system.
/// </summary>
public record UpdateSubSystemCommand(
    Guid Id,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive
) : IRequest<Result<UpdateSubSystemResponse>>;
