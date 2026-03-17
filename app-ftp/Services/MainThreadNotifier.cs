namespace app_ftp.Services;

public class MainThreadNotifier
{
    public event Action<string, bool>? StatusPublished;

    public void PublishSuccess(string message) => StatusPublished?.Invoke(message, false);

    public void PublishError(string message) => StatusPublished?.Invoke(message, true);
}
