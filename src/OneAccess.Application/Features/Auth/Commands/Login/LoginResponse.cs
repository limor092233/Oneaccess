namespace OneAccess.Application.Features.Auth.Commands.Login;

public record LoginUserInfo(Guid Id, string Username, string Email, string FullName, bool IsSystemAdministrator);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInMinutes,
    LoginUserInfo User);
