using Microsoft.AspNetCore.Authorization;

namespace OneAccess.API.Authorization;

/// <summary>
/// Authorization requirement that the authenticated user possesses a specific permission code.
/// </summary>
public record PermissionRequirement(string Permission) : IAuthorizationRequirement;
