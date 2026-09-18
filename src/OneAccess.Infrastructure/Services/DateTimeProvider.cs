using OneAccess.Application.Common.Interfaces;

namespace OneAccess.Infrastructure.Services;

/// <summary>
/// Provides system UTC date and time.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
