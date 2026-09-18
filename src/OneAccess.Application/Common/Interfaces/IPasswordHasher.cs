namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Service contract for hashing and verifying user passwords.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
