using MediatR;
using OneAccess.Application.Common.Models;

namespace OneAccess.Application.Features.Divisions.Commands.CreateDivision;

public record CreateDivisionCommand(string Name, string Description) : IRequest<Result<CreateDivisionResponse>>;
