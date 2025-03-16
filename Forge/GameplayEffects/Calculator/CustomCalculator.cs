// Copyright © 2025 Gamesmiths Guild.

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
	/// Captures the current value of a given <see cref="AttributeCaptureDefinition"/>.
	/// </summary>
	/// <param name="capturedAttribute">Definition for the attribute to be captured.</param>
	/// <param name="effect">The effect which makes use of this custom calculator.</param>
	/// <param name="target">The target of the effect.</param>
	/// <returns>The current total value of the captured attribute.</returns>
	protected static int CaptureAttributeMagnitude(
		AttributeCaptureDefinition capturedAttribute,
		GameplayEffect effect,
		IForgeEntity target)
	{
		return capturedAttribute.Source switch
		{
			AttributeCaptureSource.Source =>
				effect.Ownership.Owner.Attributes[capturedAttribute.Attribute].CurrentValue,
			AttributeCaptureSource.Target => target.Attributes[capturedAttribute.Attribute].CurrentValue,
			_ => 0,
		};
	}
}
