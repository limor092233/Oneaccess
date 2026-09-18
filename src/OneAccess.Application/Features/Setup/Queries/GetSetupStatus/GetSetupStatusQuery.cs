using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Setup.Queries.GetSetupStatus;

/// <summary>
/// Query to check if first-run System Administrator setup is required.
/// </summary>
public record GetSetupStatusQuery : IRequest<Result<SetupStatusResponse>>;
