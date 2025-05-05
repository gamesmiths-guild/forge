// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Calculator;

/// <summary>
/// Interface for creating a custom magnitude calculator.
/// </summary>
public abstract class CustomModifierMagnitudeCalculator : CustomCalculator
{
	/// <summary>
	/// Method for getting the custom calculated base magnitude.
	/// </summary>
	/// <param name="effect">The effect to be used as context for the calculation.</param>
	/// <param name="target">The target entity to be used as context for the calculation.</param>
	/// <returns>The custom calculated base magnitude.</returns>
	public abstract float CalculateBaseMagnitude(GameplayEffect effect, IForgeEntity target);
}
