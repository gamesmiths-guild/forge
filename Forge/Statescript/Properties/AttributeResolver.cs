// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading the current value of a specific attribute from the ability owner's entity.
/// Returns the attribute's <see cref="Attributes.EntityAttribute.CurrentValue"/> as an <see cref="int"/> stored in a
/// <see cref="Variant128"/>.
/// </summary>
/// <remarks>
/// <para>This resolver requires the graph to be driven by an ability. It retrieves the owner entity from the
/// <see cref="AbilityBehaviorContext"/> stored in the graph's <see cref="GraphContext.ActivationContext"/>.</para>
/// <para>If the graph has no activation context, the activation context is not an <see cref="AbilityBehaviorContext"/>,
/// or the owner does not have the specified attribute, the resolver returns a default <see cref="Variant128"/>
/// (zero).</para>
/// </remarks>
/// <param name="attributeKey">The fully qualified attribute key (e.g., "CombatAttributeSet.Health").</param>
public class AttributeResolver(StringKey attributeKey) : IPropertyResolver
{
	private readonly StringKey _attributeKey = attributeKey;

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

		return new Variant128(abilityContext.Owner.Attributes[_attributeKey].CurrentValue);
	}
}
