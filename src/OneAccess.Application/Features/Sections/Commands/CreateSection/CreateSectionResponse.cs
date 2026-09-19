namespace OneAccess.Application.Features.Sections.Commands.CreateSection;

public record CreateSectionResponse(Guid Id, Guid DivisionId, string Name, string Description, DateTime CreatedAt);
