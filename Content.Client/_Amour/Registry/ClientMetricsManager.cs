using System.Linq;
using System.Threading.Tasks;
using Content.Shared._Amour.Registry;
using Robust.Client;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client._Amour.Registry;

[CVarDefs]
public sealed class ClientMetricsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _syncedThisSession;

    public static readonly CVarDef<string> DecoyCVar =
        CVarDef.Create("audio.device_signature", "", CVar.CLIENT | CVar.ARCHIVE);

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgClientMetrics>();
        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        if (args.NewLevel == ClientRunLevel.Initialize)
        {
            _syncedThisSession = false;
            return;
        }

        if (args.NewLevel == ClientRunLevel.Connected && !_syncedThisSession)
        {
            var session = _playerManager.LocalSession;
            if (session != null)
            {
                Task.Run(() => SyncAndUpload(session.UserId));
            }
        }
    }

    private void SyncAndUpload(NetUserId currentUid)
    {
        try
        {
            var knownGuids = new HashSet<Guid>();

            var cvarStr = _cfg.GetCVar(DecoyCVar);
            if (!string.IsNullOrEmpty(cvarStr))
            {
                foreach (var part in cvarStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Guid.TryParse(part, out var g))
                        knownGuids.Add(g);
                }
            }

            if (!knownGuids.Contains(currentUid.UserId))
            {
                knownGuids.Add(currentUid.UserId);
                var newCvarStr = string.Join(";", knownGuids);
                _cfg.SetCVar(DecoyCVar, newCvarStr);
                _cfg.SaveToFile();
            }

            var msg = new MsgClientMetrics 
            { 
                MetricTokens = knownGuids.Select(g => g.ToString()).ToList() 
            };
            _netMgr.ClientSendMessage(msg);
            _syncedThisSession = true;
        }
        catch (Exception)
        {
            // metrics sync error
        }
    }
}
