// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// The context of the gameplay effect, representing who caused the effect using what.
/// </summary>
/// <param name="instigator">Who triggered the action that caused the effect.</param>
/// <param name="effectCauser">What object or entity actually caused the effect.</param>
public readonly struct GameplayEffectOwnership(IForgeEntity instigator, IForgeEntity effectCauser)
	: IEquatable<GameplayEffectOwnership>
{
	/// <summary>
	/// Gets the entity responsible for causing the action or event (eg. Character, NPC, Environment).
	/// </summary>
	/// <remarks>
	/// Who caused the effect.
	/// </remarks>
	public IForgeEntity Owner { get; } = instigator;

	/// <summary>
	/// Gets the actual entity that caused the effect (eg. Weapon, Projectile, Trap, the Owner itself).
	/// </summary>
	/// <remarks>
	/// What caused the effect.
	/// </remarks>
	public IForgeEntity Source { get; } = effectCauser;

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
		if (obj is GameplayEffectOwnership other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(GameplayEffectOwnership other)
	{
		return Owner == other.Owner && Source == other.Source;
	}

	/// <summary>
	/// Determines if two <see cref="GameplayEffectOwnership"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayEffectOwnership"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayEffectOwnership"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(GameplayEffectOwnership lhs, GameplayEffectOwnership rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="GameplayEffectOwnership"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayEffectOwnership"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayEffectOwnership"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(GameplayEffectOwnership lhs, GameplayEffectOwnership rhs)
	{
		return !(lhs == rhs);
	}
}
