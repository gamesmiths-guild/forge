// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// A set by caller float is a magnitude used for allowing the caller to set a specific value before applying an effect.
/// </summary>
/// <param name="tag">The tag used to identify this custom magnitude.</param>
/// <remarks>
/// A <see cref="Tags.Tag"/> is used for mapping different possible custom values.
/// </remarks>
public readonly struct SetByCallerFloat(Tag tag) : IEquatable<SetByCallerFloat>
{
	/// <summary>
	/// Gets the tag used for identifying the custom value set.
	/// </summary>
	public readonly Tag Tag { get; } = tag;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return Tag.GetHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is SetByCallerFloat other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(SetByCallerFloat other)
	{
		return Tag == other.Tag;
	}

	/// <summary>
	/// Determines if two <see cref="SetByCallerFloat"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="SetByCallerFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="SetByCallerFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(SetByCallerFloat lhs, SetByCallerFloat rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="SetByCallerFloat"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="SetByCallerFloat"/> to compare.</param>
	/// <param name="rhs">The second <see cref="SetByCallerFloat"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(SetByCallerFloat lhs, SetByCallerFloat rhs)
	{
		return !(lhs == rhs);
	}
}
