// Copyright © 2024 Gamesmiths Guild.

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// Interface for creating a custom magnitude calculator.
/// </summary>
public interface IMagnitudeCalculator
{
	/// <summary>
	/// Method for getting the custom calculated base magnitude.
	/// </summary>
	/// <param name="effect">The effect to be used as context for the calculation.</param>
	/// <returns>The custom calculated base magnitude.</returns>
	float CalculateBaseMagnitude(GameplayEffect effect);
}
