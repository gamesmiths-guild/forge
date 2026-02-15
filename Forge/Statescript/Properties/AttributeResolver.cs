// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading the current value of a specific attribute from the graph owner's entity.
/// Returns the attribute's <see cref="Attributes.EntityAttribute.CurrentValue"/> as an <see cref="int"/> stored in a
/// <see cref="Variant128"/>.
/// </summary>
/// <remarks>
/// If the graph context has no owner or the owner does not have the specified attribute, the resolver returns a default
/// <see cref="Variant128"/> (zero).
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
		if (graphContext.Owner is null)
		{
			return default;
		}

		if (!graphContext.Owner.Attributes.ContainsAttribute(_attributeKey))
		{
			return default;
		}

		return new Variant128(graphContext.Owner.Attributes[_attributeKey].CurrentValue);
	}
}
