using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared._Amour.Loadouts.Effects;

/// <summary>
/// Loadout effect that restricts the loadout to Boosty subscribers with a specific tier level.
/// The tier level is synced from Discord via the DiscordBot and stored in amour_boosters table.
/// </summary>
public sealed partial class BoostyTierLoadoutEffect : LoadoutEffect
{
    /// <summary>
    /// Minimum tier level required to access this loadout.
    /// Higher tier levels have access to lower tier loadouts.
    /// Example: If MinTierLevel is 2, users with tier 2, 3, 4... can access it.
    /// </summary>
    [DataField]
    public int MinTierLevel = 1;

    /// <summary>
    /// Optional: Specific tier names that can access this loadout.
    /// If specified, only these exact tier names will have access (ignores MinTierLevel).
    /// Case-insensitive comparison is used.
    /// </summary>
    [DataField]
    public List<string>? AllowedTiers;

    /// <summary>
    /// Reason shown to users who don't have access.
    /// If not specified, a default message will be shown.
    /// </summary>
    [DataField]
    public string? DeniedReason;

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
            reason = FormattedMessage.FromMarkupOrThrow(
                Loc.GetString("loadout-effect-Boosty-no-session"));
            return false;
        }

        // Try to get the booster tier manager from IoC (may not exist on client)
        if (!collection.TryResolveType<IBoostyTierManager>(out var tierManager))
        {
            // On client side, tier manager is not available - allow and let server validate
            return true;
        }

        var tierInfo = tierManager.GetPlayerTier(session);

        if (tierInfo == null || !tierInfo.IsActive)
        {
            reason = FormattedMessage.FromMarkupOrThrow(
                DeniedReason ?? Loc.GetString("loadout-effect-Boosty-no-subscription"));
            return false;
        }

        // Check specific tier names if specified
        if (AllowedTiers != null && AllowedTiers.Count > 0)
        {
            var hasAllowedTier = tierInfo.TierName != null && 
                AllowedTiers.Any(t => string.Equals(t, tierInfo.TierName, StringComparison.OrdinalIgnoreCase));
            
            if (!hasAllowedTier)
            {
                reason = FormattedMessage.FromMarkupOrThrow(
                    DeniedReason ?? Loc.GetString("loadout-effect-Boosty-tier-required", 
                        ("tiers", string.Join(", ", AllowedTiers))));
                return false;
            }
            
            return true;
        }

        // Check minimum tier level
        if (tierInfo.TierLevel < MinTierLevel)
        {
            reason = FormattedMessage.FromMarkupOrThrow(
                DeniedReason ?? Loc.GetString("loadout-effect-Boosty-higher-tier-required",
                    ("required", MinTierLevel),
                    ("current", tierInfo.TierLevel)));
            return false;
        }

        return true;
    }
}

/// <summary>
/// Interface for accessing Boosty tier information.
/// Implemented on the server side to read from database.
/// </summary>
public interface IBoostyTierManager
{
    /// <summary>
    /// Gets the Boosty tier info for a player.
    /// </summary>
    BoostyPlayerTier? GetPlayerTier(ICommonSession session);
}

/// <summary>
/// Boosty tier information for a player.
/// </summary>
public sealed class BoostyPlayerTier
{
    /// <summary>
    /// Name of the tier (e.g., "Tier1", "Gold", "VIP").
    /// </summary>
    public string? TierName { get; init; }
    
    /// <summary>
    /// Numeric level of the tier for comparison (higher = better).
    /// </summary>
    public int TierLevel { get; init; }
    
    /// <summary>
    /// Whether the subscription is currently active.
    /// </summary>
    public bool IsActive { get; init; }
}
