using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Commands.RevokeUserDivision;

public record RevokeUserDivisionCommand(Guid DivisionId, Guid UserId) : IRequest<Result>;
