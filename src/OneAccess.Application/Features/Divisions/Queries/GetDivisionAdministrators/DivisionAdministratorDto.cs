namespace OneAccess.Application.Features.Divisions.Queries.GetDivisionAdministrators;

public record DivisionAdministratorDto(Guid UserId, string Username, string FullName, string Email, DateTime GrantedAt);
