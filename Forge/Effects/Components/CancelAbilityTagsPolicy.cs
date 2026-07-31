// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Specifies when a <see cref="CancelAbilityTagsEffectComponent"/> cancels the target's matching abilities.
/// </summary>
public enum CancelAbilityTagsPolicy : byte
{
	/// <summary>
	/// Cancels once, when the effect is applied. Applies to every effect, including instant ones, and fires again for
	/// each successfully applied stack.
	/// </summary>
	OnApplication = 0,

	/// <summary>
	/// Cancels on each execution. Only instant and periodic effects are executed, so a duration effect using this
	/// policy must be periodic.
	/// </summary>
	OnExecution = 1,
}
