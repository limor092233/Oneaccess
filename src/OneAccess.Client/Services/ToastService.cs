namespace OneAccess.Client.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; } = ToastLevel.Info;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowToast(string message, ToastLevel level = ToastLevel.Info)
    {
        OnShow?.Invoke(new ToastMessage { Message = message, Level = level });
    }

    public void ShowError(string message) => ShowToast(message, ToastLevel.Error);
    public void ShowSuccess(string message) => ShowToast(message, ToastLevel.Success);
    public void ShowWarning(string message) => ShowToast(message, ToastLevel.Warning);
    public void ShowInfo(string message) => ShowToast(message, ToastLevel.Info);
}
