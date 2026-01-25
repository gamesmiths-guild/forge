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
	internal bool CanApply(Effect effect, IForgeEntity target, int level)
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
