using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Commands.AssignUserDivision;

public record AssignUserDivisionCommand(Guid DivisionId, Guid UserId) : IRequest<Result>;
