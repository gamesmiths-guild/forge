// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Periodic;

/// <summary>
/// The periodic data for a Gameplay Effect.
/// </summary>
/// <param name="period">The duration for the periodic triggers for the effect.</param>
/// <param name="executeOnApplication">If the periodic effect should trigger on application.</param>
/// <param name="periodInhibitionRemovedPolicy">How to handle periods when inhibition is removed.</param>
public readonly struct PeriodicData(
	ScalableFloat period,
	bool executeOnApplication,
	PeriodInhibitionRemovedPolicy periodInhibitionRemovedPolicy) : IEquatable<PeriodicData>
{
	/// <summary>
	/// Gets the period duration for the periodic effect.
	/// </summary>
	public ScalableFloat Period { get; } = period;

	/// <summary>
	/// Gets a value indicating whether this effect should execute its periodic effect on application or not.
	/// </summary>
	public bool ExecuteOnApplication { get; } = executeOnApplication;

	/// <summary>
	/// Gets a value for the policy to use when the period inhibition is removed.
	/// </summary>
	public PeriodInhibitionRemovedPolicy PeriodInhibitionRemovedPolicy { get; } = periodInhibitionRemovedPolicy;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Period);
		hash.Add(ExecuteOnApplication);
		hash.Add(PeriodInhibitionRemovedPolicy);

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is PeriodicData other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(PeriodicData other)
	{
		return Period == other.Period
			&& ExecuteOnApplication.Equals(other.ExecuteOnApplication)
			&& PeriodInhibitionRemovedPolicy == other.PeriodInhibitionRemovedPolicy;
	}

	/// <summary>
	/// Determines if two <see cref="PeriodicData"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="PeriodicData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="PeriodicData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(PeriodicData lhs, PeriodicData rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="PeriodicData"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="PeriodicData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="PeriodicData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(PeriodicData lhs, PeriodicData rhs)
	{
		return !(lhs == rhs);
	}
}
