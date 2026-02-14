using System.Collections.Concurrent;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Amour.Loadouts.Effects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server._Amour.Loadouts;

public sealed class BoostyTierManager : IBoostyTierManager
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill? _sawmill;

    private readonly ConcurrentDictionary<Guid, (BoostyPlayerTier? Tier, DateTime CachedAt)> _cache = new();

    private readonly ConcurrentDictionary<Guid, Task> _inflight = new();

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public BoostyPlayerTier? GetPlayerTier(ICommonSession session)
    {
        _sawmill ??= _log.GetSawmill("amour.boosty");

        var userId = session.UserId;

        if (_cache.TryGetValue(userId, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < CacheDuration)
                return cached.Tier;

            EnsureRefresh(userId);
            return cached.Tier;
        }

        EnsureRefresh(userId);
        return null;
    }

    private void EnsureRefresh(Guid userId)
    {
        if (_inflight.ContainsKey(userId))
            return;

        var task = RefreshAsync(userId);
        if (!_inflight.TryAdd(userId, task))
            return;

        _ = task.ContinueWith(t =>
        {
            _inflight.TryRemove(userId, out _);

            if (t.IsFaulted)
                _sawmill?.Warning($"Failed to refresh Boosty tier for {userId}: {t.Exception}");
        });
    }

    private async Task RefreshAsync(Guid userId)
    {
        try
        {
            var boosterInfo = await _db.GetBoostyTierAsync(userId);

            BoostyPlayerTier? tier = null;
            if (boosterInfo != null)
            {
                tier = new BoostyPlayerTier
                {
                    TierName = boosterInfo.TierName,
                    TierLevel = boosterInfo.TierLevel,
                    IsActive = boosterInfo.IsActive
                };
            }

            _cache[userId] = (tier, DateTime.UtcNow);
        }
        catch (Exception e)
        {
            _cache[userId] = (null, DateTime.UtcNow);
            _sawmill?.Warning($"Error querying Boosty tier for {userId}: {e}");
        }
    }

    public void InvalidateCache(Guid playerId)
    {
        _cache.TryRemove(playerId, out _);
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
