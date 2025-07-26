// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Cues;

/// <summary>
/// The possible types of magnitude that can be used for a cue.
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
	AttributeValueChange = 2,

	/// <summary>
	/// The base value of the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeBaseValue = 3,

	/// <summary>
	/// The current value of the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeCurrentValue = 4,

	/// <summary>
	/// The current modifier value applied to the target attribute will be used as the cue's magnitude.
	/// </summary>
	/// <remarks>
	/// Note that even modifiers from other effects will take effect here, not just the modifiers from the effect which
	/// triggered the cue.
	/// </remarks>
	AttributeModifier = 5,

	/// <summary>
	/// The current overflow value applied to the target attribute will be used as the cue's magnitude.
	/// </summary>
	/// <remarks>
	/// Note that even overflow from other effects will take effect here, not just the modifiers from the effect which
	/// triggered the cue.
	/// </remarks>
	AttributeOverflow = 6,

	/// <summary>
	/// The valid modifier value applied to the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeValidModifier = 7,

	/// <summary>
	/// The minimum value of the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeMin = 8,

	/// <summary>
	/// The maximum value of the target attribute will be used as the cue's magnitude.
	/// </summary>
	AttributeMax = 9,

	/// <summary>
	/// The magnitude of the target attribute evaluated up to a specific channel will be used as the cue's magnitude.
	/// </summary>
	AttributeMagnitudeEvaluatedUpToChannel = 10,
}
