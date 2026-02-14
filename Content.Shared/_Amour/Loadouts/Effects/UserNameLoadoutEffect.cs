using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared._Amour.Loadouts.Effects;

/// <summary>
/// Loadout effect that restricts the loadout to specific users by their SS14 username.
/// Used for personal/sponsor items that should only be available to specific players.
/// </summary>
public sealed partial class UserNameLoadoutEffect : LoadoutEffect
{
    /// <summary>
    /// List of usernames that can use this loadout.
    /// Case-insensitive comparison is used.
    /// </summary>
    [DataField(required: true)]
    public List<string> AllowedUsers = new();

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        // If no session, deny
        if (session == null)
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-effect-username-denied"));
            return false;
        }

        var userName = session.Name;

        // Handle localhost@ prefix
        if (userName.StartsWith("localhost@", StringComparison.OrdinalIgnoreCase))
            userName = userName.Substring("localhost@".Length);

        foreach (var allowedUser in AllowedUsers)
        {
            if (string.Equals(userName, allowedUser, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-effect-username-denied"));
        return false;
    }
}
