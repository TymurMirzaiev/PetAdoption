namespace PetAdoption.Web.BlazorApp.Services;

public interface IChatUnreadService : IDisposable
{
    int MyUnreadCount { get; }
    int OrgUnreadCount { get; }
    event Action? OnChange;
    void StartPolling(string? orgId);
}

public class ChatUnreadService : IChatUnreadService
{
    private readonly PetApiClient _petApi;
    private Timer? _timer;
    private string? _orgId;

    public int MyUnreadCount { get; private set; }
    public int OrgUnreadCount { get; private set; }
    public event Action? OnChange;

    public ChatUnreadService(PetApiClient petApi) => _petApi = petApi;

    public void StartPolling(string? orgId)
    {
        _orgId = orgId;
        _timer = new Timer(async _ =>
        {
            var changed = false;

            try
            {
                var count = await _petApi.GetMyChatUnreadTotalAsync();
                if (count != MyUnreadCount)
                {
                    MyUnreadCount = count;
                    changed = true;
                }
            }
            catch { /* swallow */ }

            if (!string.IsNullOrEmpty(_orgId) && Guid.TryParse(_orgId, out var oid))
            {
                try
                {
                    var count = await _petApi.GetOrgChatUnreadTotalAsync(oid);
                    if (count != OrgUnreadCount)
                    {
                        OrgUnreadCount = count;
                        changed = true;
                    }
                }
                catch { /* swallow */ }
            }

            if (changed)
                OnChange?.Invoke();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    public void Dispose() => _timer?.Dispose();
}
