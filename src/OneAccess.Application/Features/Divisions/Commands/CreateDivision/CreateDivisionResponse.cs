namespace OneAccess.Application.Features.Divisions.Commands.CreateDivision;

public record CreateDivisionResponse(Guid Id, string Name, string Description, DateTime CreatedAt);
