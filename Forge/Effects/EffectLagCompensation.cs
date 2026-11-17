// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// How an effect counts for a lag-compensated hit, which the server tests against the world as its instigator saw it.
/// </summary>
public enum EffectLagCompensation : byte
{
	/// <summary>
	/// The effect counts as it is when the server tests the hit: an invulnerability started after the action still
	/// protects. The default.
	/// </summary>
	Current = 0,

	/// <summary>
	/// The effect counts as it was at the tick the instigator was shown, for whether the hit lands: the tags it grants
	/// and its immunity, as the application checks them. A dodge started after the action doesn't save the target,
	/// and an invulnerability that ended after it still does. What the hit then does reads the target as it is.
	/// </summary>
	Rewound = 1,
}
