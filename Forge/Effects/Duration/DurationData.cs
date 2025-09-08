// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Duration;

/// <summary>
/// Duration data for a effect.
/// </summary>
/// <param name="DurationType">The type of duration for the effect.</param>
/// <param name="Duration">The duration for this effect in case it's of type <see cref="DurationType.HasDuration"/>.
/// </param>
public readonly record struct DurationData(DurationType DurationType, ScalableFloat? Duration = null);
