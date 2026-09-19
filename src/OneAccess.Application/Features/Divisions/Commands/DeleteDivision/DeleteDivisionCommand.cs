using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Commands.DeleteDivision;

public record DeleteDivisionCommand(Guid Id) : IRequest<Result>;
