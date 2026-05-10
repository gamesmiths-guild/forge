// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Effects.Magnitudes;

internal static class AttributeCalculationTypeExtensions
{
	internal static int ResolveValue(
		this AttributeCalculationType attributeCalculationType,
		EntityAttribute attribute,
		int finalChannel = 0)
	{
		return attributeCalculationType switch
		{
			AttributeCalculationType.CurrentValue => attribute.CurrentValue,
			AttributeCalculationType.BaseValue => attribute.BaseValue,
			AttributeCalculationType.Modifier => attribute.Modifier,
			AttributeCalculationType.Overflow => attribute.Overflow,
			AttributeCalculationType.ValidModifier => attribute.ValidModifier,
			AttributeCalculationType.Min => attribute.Min,
			AttributeCalculationType.Max => attribute.Max,
			AttributeCalculationType.MagnitudeEvaluatedUpToChannel =>
				(int)attribute.CalculateMagnitudeUpToChannel(finalChannel),
			_ => throw new ArgumentOutOfRangeException(
				nameof(attributeCalculationType),
				attributeCalculationType,
				$"Unsupported {nameof(AttributeCalculationType)} value."),
		};
	}
}
