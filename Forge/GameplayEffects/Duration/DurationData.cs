// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Duration;

/// <summary>
/// Duration data for a Gameplay Effect.
/// </summary>
/// <param name="durationType">The type of duration for the effect.</param>
/// <param name="duration">The duration for this effect in case it's of type <see cref="DurationType.HasDuration"/>.
/// </param>
public readonly struct DurationData(DurationType durationType, ScalableFloat? duration = null)
{
	/// <summary>
	/// Gets the type of duration for the Gameplay Effect.
	/// </summary>
	public DurationType Type { get; } = durationType;

	/// <summary>
	/// Gets the duration of the Gameplay Effects.
	/// </summary>
	/// <remarks>
	/// This is only valid for effects with <see cref="DurationType.HasDuration"/>. <see langword="null"/> otherwise.
	/// </remarks>
	public ScalableFloat? Duration { get; } = duration;
}
