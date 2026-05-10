// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading a selected value from a specific attribute on the ability owner's entity.
/// </summary>
/// <remarks>
/// <para>This resolver requires the graph to be driven by an ability. It retrieves the owner entity from the
/// <see cref="AbilityBehaviorContext"/> stored in the graph's <see cref="GraphContext.ActivationContext"/>.</para>
/// <para>If the graph has no activation context, the activation context is not an <see cref="AbilityBehaviorContext"/>,
/// or the owner does not have the specified attribute, the resolver returns a default <see cref="Variant128"/>
/// (zero).</para>
/// </remarks>
/// <param name="attributeKey">The fully qualified attribute key (e.g., "CombatAttributeSet.Health").</param>
/// <param name="attributeCalculationType">Which value to read from the attribute. Defaults to
/// <see cref="AttributeCalculationType.CurrentValue"/>.</param>
/// <param name="finalChannel">Only used when <paramref name="attributeCalculationType"/> is
/// <see cref="AttributeCalculationType.MagnitudeEvaluatedUpToChannel"/>.</param>
public class AttributeResolver(
	StringKey attributeKey,
	AttributeCalculationType attributeCalculationType = AttributeCalculationType.CurrentValue,
	int finalChannel = 0) : IPropertyResolver
{
	private readonly StringKey _attributeKey = attributeKey;
	private readonly AttributeCalculationType _attributeCalculationType = attributeCalculationType;

	private readonly int _finalChannel = finalChannel >= 0
		? finalChannel
		: throw new ArgumentOutOfRangeException(nameof(finalChannel), "finalChannel cannot be negative.");

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return default;
		}

		if (!abilityContext.Owner.Attributes.ContainsAttribute(_attributeKey))
		{
			return default;
		}

		return new Variant128(
			_attributeCalculationType.ResolveValue(
				abilityContext.Owner.Attributes[_attributeKey],
				_finalChannel));
	}
}
