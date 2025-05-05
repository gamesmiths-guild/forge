// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Duration;

/// <summary>
/// Duration data for a Gameplay Effect.
/// </summary>
/// <param name="durationType">The type of duration for the effect.</param>
/// <param name="duration">The duration for this effect in case it's of type <see cref="DurationType.HasDuration"/>.
/// </param>
public readonly struct DurationData(DurationType durationType, ScalableFloat? duration = null)
	: IEquatable<DurationData>
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

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Type);
		hash.Add(Duration);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is DurationData other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(DurationData other)
	{
		return Type == other.Type
			&& Duration.Equals(other.Duration);
	}

	/// <summary>
	/// Determines if two <see cref="DurationData"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="DurationData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="DurationData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(DurationData lhs, DurationData rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="DurationData"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="DurationData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="DurationData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(DurationData lhs, DurationData rhs)
	{
		return !(lhs == rhs);
	}
}
