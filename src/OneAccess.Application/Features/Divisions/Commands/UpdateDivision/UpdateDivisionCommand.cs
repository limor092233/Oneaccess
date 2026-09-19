using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Commands.UpdateDivision;

public record UpdateDivisionCommand(Guid Id, string Name, string Description) : IRequest<Result>;
