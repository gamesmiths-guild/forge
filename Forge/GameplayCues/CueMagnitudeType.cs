// Copyright © 2025 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayCues;

/// <summary>
/// The possible types of magnitude that can be used for a gameplay cue.
/// </summary>
public enum CueMagnitudeType
{
	/// <summary>
	/// The effect level will be used as the magnitude.
	/// </summary>
	EffectLevel = 0,

	/// <summary>
	/// The effect stack count will be used as the magnitude.
	/// </summary>
	StackCount = 1,

	/// <summary>
	/// The delta applied to the a target attribute from the effect that triggered the cue will be used as the cue's
	/// magnitude.
	/// </summary>
	AttributeDelta = 2,

	/// <summary>
	/// The current value of the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeCurrentValue = 3,

	/// <summary>
	/// The current modifier value applied to the target attribute will be used as the cue's magnitude.
	/// </summary>
	/// <remarks>
	/// Note that even modifiers from other effects will take effect here, not just the modifiers from the effect which
	/// triggered the cue.
	/// </remarks>
	AttributeModifier = 4,
}
