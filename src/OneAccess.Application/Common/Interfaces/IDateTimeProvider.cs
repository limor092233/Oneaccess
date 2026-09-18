namespace OneAccess.Application.Common.Interfaces;

/// <summary>
/// Abstraction for accessing current date and time in UTC.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
