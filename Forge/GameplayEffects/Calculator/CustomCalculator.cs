// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Calculator;

/// <summary>
/// Abstract class for implementing custom logic for calculating attribute modifiers.
/// </summary>
public abstract class CustomCalculator
{
	/// <summary>
	/// Gets a list of the relevant attributes to be captured and used by this calculator.
	/// </summary>
	public List<AttributeCaptureDefinition> AttributesToCapture { get; } = [];

	/// <summary>
	/// Gets a dictionary of custom cue parameters.
	/// </summary>
	public Dictionary<StringKey, object> CustomCueParameters { get; } = [];

	/// <summary>
	/// Captures the current value of a given <see cref="AttributeCaptureDefinition"/>.
	/// </summary>
	/// <param name="capturedAttribute">Definition for the attribute to be captured.</param>
	/// <param name="effect">The effect which makes use of this custom calculator.</param>
	/// <param name="target">The target of the effect.</param>
	/// <returns>The current total value of the captured attribute.</returns>
	protected static int CaptureAttributeMagnitude(
		AttributeCaptureDefinition capturedAttribute,
		GameplayEffect effect,
		IForgeEntity? target)
	{
		switch (capturedAttribute.Source)
		{
			case AttributeCaptureSource.Source:

				if (effect.Ownership.Owner?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
				{
					return 0;
				}

				return effect.Ownership.Owner.Attributes[capturedAttribute.Attribute].CurrentValue;

			case AttributeCaptureSource.Target:

				if (target?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
				{
					return 0;
				}

				return target.Attributes[capturedAttribute.Attribute].CurrentValue;
		}

		return 0;
	}
}
