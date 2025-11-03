// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Modifiers;

/// <summary>
/// A modifier that affects an attribute with the given configuration.
/// </summary>
/// <param name="Attribute">The target attribute to be modified.</param>
/// <param name="Operation">The type of operation to be used.</param>
/// <param name="Magnitude">The magnitude calculation and configurations to be used.</param>
/// <param name="Channel">The channel to be affected by this modifier.</param>
public readonly record struct Modifier(
	StringKey Attribute,
	ModifierOperation Operation,
	ModifierMagnitude Magnitude,
	int Channel = 0)
{
	/// <summary>
	/// Checks whether this modifier can be applied to the given target entity at the specified level.
	/// </summary>
	/// <param name="effect">The source effect of this modifier.</param>
	/// <param name="target">The target entity to check against.</param>
	/// <param name="level">The level to be used for magnitude calculation.</param>
	/// <returns><see langword="true"/> if the modifier can be applied; otherwise, <see langword="false"/>.</returns>
	public bool CanApply(Effect effect, IForgeEntity target, int level)
	{
		if (!target.Attributes.ContainsAttribute(Attribute))
		{
			return false;
		}

		var magnitude = Magnitude.GetMagnitude(effect, target, level);

		EntityAttribute attribute = target.Attributes[Attribute];

		if (magnitude < 0)
		{
			return magnitude >= attribute.Min - attribute.CurrentValue;
		}

		if (magnitude > 0)
		{
			return magnitude <= attribute.Max - attribute.CurrentValue;
		}

		return true;
	}
}
