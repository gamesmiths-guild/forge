// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading a selected value from a specific attribute on a resolved entity.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="OwnerEntityResolver"/>.</para>
/// <para>If the selected entity is not available or does not have the specified attribute, the resolver returns a
/// default <see cref="Variant128"/> (zero).</para>
/// </remarks>
/// <param name="attributeKey">The fully qualified attribute key.</param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
/// <param name="attributeCalculationType">Which value to read from the attribute.</param>
/// <param name="finalChannel">The final channel for channel-limited evaluation.</param>
public class AttributeResolver(
	StringKey attributeKey,
	IEntityResolver entityResolver,
	AttributeCalculationType attributeCalculationType = AttributeCalculationType.CurrentValue,
	int finalChannel = 0) : IPropertyResolver
{
	private static readonly IEntityResolver _defaultEntityResolver = new OwnerEntityResolver();

	private readonly StringKey _attributeKey = attributeKey;

	private readonly IEntityResolver _entityResolver = entityResolver
		?? throw new ArgumentNullException(nameof(entityResolver));

	private readonly AttributeCalculationType _attributeCalculationType = attributeCalculationType;

	private readonly int _finalChannel = finalChannel >= 0
		? finalChannel
		: throw new ArgumentOutOfRangeException(nameof(finalChannel), "finalChannel cannot be negative.");

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <summary>
	/// Initializes a new instance of the <see cref="AttributeResolver"/> class targeting the owner entity.
	/// </summary>
	/// <param name="attributeKey">The fully qualified attribute key.</param>
	public AttributeResolver(StringKey attributeKey)
		: this(attributeKey, _defaultEntityResolver)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AttributeResolver"/> class targeting the owner entity.
	/// </summary>
	/// <param name="attributeKey">The fully qualified attribute key.</param>
	/// <param name="attributeCalculationType">Which value to read from the attribute.</param>
	public AttributeResolver(StringKey attributeKey, AttributeCalculationType attributeCalculationType)
		: this(attributeKey, _defaultEntityResolver, attributeCalculationType)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AttributeResolver"/> class targeting the owner entity.
	/// </summary>
	/// <param name="attributeKey">The fully qualified attribute key.</param>
	/// <param name="attributeCalculationType">Which value to read from the attribute.</param>
	/// <param name="finalChannel">The final channel for channel-limited evaluation.</param>
	public AttributeResolver(
		StringKey attributeKey,
		AttributeCalculationType attributeCalculationType,
		int finalChannel)
		: this(attributeKey, _defaultEntityResolver, attributeCalculationType, finalChannel)
	{
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);
		if (entity?.Attributes.ContainsAttribute(_attributeKey) != true)
		{
			return default;
		}

		return new Variant128(
			_attributeCalculationType.ResolveValue(
				entity.Attributes[_attributeKey],
				_finalChannel));
	}
}
