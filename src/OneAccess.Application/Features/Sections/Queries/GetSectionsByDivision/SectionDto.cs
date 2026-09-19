namespace OneAccess.Application.Features.Sections.Queries.GetSectionsByDivision;

public record SectionDto(Guid Id, Guid DivisionId, string Name, string Description, DateTime CreatedAt);
