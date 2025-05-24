// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// The context of the effect, representing who caused the effect using what.
/// </summary>
/// <param name="owner">Who triggered the action that caused the effect.</param>
/// <param name="effectCauser">What object or entity actually caused the effect.</param>
public readonly struct EffectOwnership(IForgeEntity? owner, IForgeEntity? effectCauser) : IEquatable<EffectOwnership>
{
	/// <summary>
	/// Gets the entity responsible for causing the action or event (eg. Character, NPC, Environment).
	/// </summary>
	/// <remarks>
	/// Who caused the effect.
	/// </remarks>
	public IForgeEntity? Owner { get; } = owner;

	/// <summary>
	/// Gets the actual entity that caused the effect (eg. Weapon, Projectile, Trap, the Owner itself).
	/// </summary>
	/// <remarks>
	/// What caused the effect.
	/// </remarks>
	public IForgeEntity? Source { get; } = effectCauser;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Owner);
		hash.Add(Source);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is EffectOwnership other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(EffectOwnership other)
	{
		return Owner == other.Owner && Source == other.Source;
	}

	/// <summary>
	/// Determines if two <see cref="EffectOwnership"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="EffectOwnership"/> to compare.</param>
	/// <param name="rhs">The second <see cref="EffectOwnership"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(EffectOwnership lhs, EffectOwnership rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="EffectOwnership"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="EffectOwnership"/> to compare.</param>
	/// <param name="rhs">The second <see cref="EffectOwnership"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(EffectOwnership lhs, EffectOwnership rhs)
	{
		return !(lhs == rhs);
	}
}
