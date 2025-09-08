// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Periodic;

/// <summary>
/// The periodic data for a effect.
/// </summary>
/// <param name="Period">The duration for the periodic triggers for the effect.</param>
/// <param name="ExecuteOnApplication">If the periodic effect should trigger on application.</param>
/// <param name="PeriodInhibitionRemovedPolicy">How to handle periods when inhibition is removed.</param>
public readonly record struct PeriodicData(
	ScalableFloat Period,
	bool ExecuteOnApplication,
	PeriodInhibitionRemovedPolicy PeriodInhibitionRemovedPolicy);
