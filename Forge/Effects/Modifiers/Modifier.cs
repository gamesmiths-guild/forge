// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Modifiers;

/// <summary>
/// A modifier that affects an attribute with the given configuration.
/// </summary>
/// <param name="attribute">The target attribute to be modified.</param>
/// <param name="operation">The type of operation to be used.</param>
/// <param name="magnitude">The magnitude calculation and configurations to be used.</param>
/// <param name="channel">The channel to be affected by this modifier.</param>
public readonly struct Modifier(
	StringKey attribute,
	ModifierOperation operation,
	ModifierMagnitude magnitude,
	int channel = 0) : IEquatable<Modifier>
{
	/// <summary>
	/// Gets the attribute being modified by this modifier.
	/// </summary>
	public StringKey Attribute { get; } = attribute;

	/// <summary>
	/// Gets the type of operation this modifier is using.
	/// </summary>
	public ModifierOperation Operation { get; } = operation;

	/// <summary>
	/// Gets the magnitude calculation and configurations this modifier is using.
	/// </summary>
	public ModifierMagnitude Magnitude { get; } = magnitude;

	/// <summary>
	/// Gets which channel this modifier is being applied to.
	/// </summary>
	public int Channel { get; } = channel;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Attribute);
		hash.Add(Operation);
		hash.Add(Magnitude);
		hash.Add(Channel);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is Modifier other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(Modifier other)
	{
		return Attribute == other.Attribute
			&& Operation.Equals(other.Operation)
			&& Magnitude.Equals(other.Magnitude)
			&& Channel.Equals(other.Channel);
	}

	/// <summary>
	/// Determines if two <see cref="Modifier"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="Modifier"/> to compare.</param>
	/// <param name="rhs">The second <see cref="Modifier"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(Modifier lhs, Modifier rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="Modifier"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="Modifier"/> to compare.</param>
	/// <param name="rhs">The second <see cref="Modifier"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(Modifier lhs, Modifier rhs)
	{
		return !(lhs == rhs);
	}
}
