namespace OneAccess.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested domain entity cannot be found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
