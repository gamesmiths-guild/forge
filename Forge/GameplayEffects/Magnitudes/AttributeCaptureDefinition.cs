// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Attribute = Gamesmiths.Forge.Attributes.Attribute;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes;

/// <summary>
/// Set of data that definies how an attribute is going to be captured.
/// </summary>
/// <param name="attribute">Which attribute to capture.</param>
/// <param name="source">From what target to capture the attribute from.</param>
/// <param name="snapshop">Whether the captured value should be a snapshot or not.</param>
public readonly struct AttributeCaptureDefinition(
	StringKey attribute,
	AttributeCaptureSource source,
	bool snapshop = true) : IEquatable<AttributeCaptureDefinition>
{
	/// <summary>
	/// Gets a key indicating which attribute is being captured.
	/// </summary>
	public StringKey Attribute { get; } = attribute;

	/// <summary>
	/// Gets the source entity from where the attribute is being captured from.
	/// </summary>
	public AttributeCaptureSource Source { get; } = source;

	/// <summary>
	/// Gets a value indicating whether snapshot is enabled. A snapshot indicates that the value is saved at the moment
	/// of effect creation. Non-snapshop captures are updated when the target attribute is updated.
	/// </summary>
	/// <remarks>
	/// Only effects with <see cref="Duration.DurationType"/> == <see cref="Duration.DurationType.Infinite"/> or
	/// <see cref="Duration.DurationType"/> == <see cref="Duration.DurationType.HasDuration"/> can snapshot.
	/// </remarks>
	public bool Snapshot { get; } = snapshop;

	/// <summary>
	/// Retrieves the captured attribute for a given entity.
	/// </summary>
	/// <param name="source">The source entity to get the attribute from.</param>
	/// <returns>The captured attribute.</returns>
	public readonly Attribute GetAttribute(IForgeEntity source)
	{
		return source.Attributes[Attribute];
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Attribute);
		hash.Add(Source);
		hash.Add(Snapshot);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is AttributeCaptureDefinition other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(AttributeCaptureDefinition other)
	{
		return Attribute.Equals(other.Attribute)
			&& Source.Equals(other.Source)
			&& Snapshot == other.Snapshot;
	}

	/// <summary>
	/// Determines if two <see cref="AttributeCaptureDefinition"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="AttributeCaptureDefinition"/> to compare.</param>
	/// <param name="rhs">The second <see cref="AttributeCaptureDefinition"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(AttributeCaptureDefinition lhs, AttributeCaptureDefinition rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="AttributeCaptureDefinition"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="AttributeCaptureDefinition"/> to compare.</param>
	/// <param name="rhs">The second <see cref="AttributeCaptureDefinition"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(AttributeCaptureDefinition lhs, AttributeCaptureDefinition rhs)
	{
		return !(lhs == rhs);
	}
}
