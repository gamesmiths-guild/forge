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
/// <param name="AggregationMode">How this modifier combines with the other modifiers affecting the same attribute,
/// channel and operation.</param>
public readonly record struct Modifier(
	StringKey Attribute,
	ModifierOperation Operation,
	ModifierMagnitude Magnitude,
	int Channel = 0,
	AggregationMode AggregationMode = AggregationMode.Sum)
{
	internal bool CanApply(Effect effect, IForgeEntity target, int level)
	{
		if (!target.Attributes.ContainsAttribute(Attribute))
		{
			return false;
		}

		float magnitude = Magnitude.GetMagnitude(effect, target, level);

		EntityAttribute attribute = target.Attributes[Attribute];

		// Only a flat modifier's magnitude is a delta. The other operations describe the resulting value, so their
		// delta has to be derived before it can be checked against the attribute's bounds — otherwise an override to
		// a value well within range reads as an unaffordable change.
		float delta = Operation switch
		{
			ModifierOperation.PercentBonus =>
				(int)(attribute.CurrentValue * Math.Round(1 + magnitude, 6)) - attribute.CurrentValue,
			ModifierOperation.Override => magnitude - attribute.CurrentValue,
			_ => magnitude,
		};

		if (delta < 0)
		{
			return delta >= attribute.Min - attribute.CurrentValue;
		}

		if (delta > 0)
		{
			return delta <= attribute.Max - attribute.CurrentValue;
		}

		return true;
	}
}
