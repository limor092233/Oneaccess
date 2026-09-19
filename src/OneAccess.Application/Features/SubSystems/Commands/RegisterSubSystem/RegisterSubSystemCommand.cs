using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.SubSystems.Commands.RegisterSubSystem;

/// <summary>
/// Command to register a new external sub-system.
/// </summary>
public record RegisterSubSystemCommand(
    string Code,
    string Name,
    string BaseUrl,
    string Audience,
    bool IsActive = true
) : IRequest<Result<RegisterSubSystemResponse>>;
