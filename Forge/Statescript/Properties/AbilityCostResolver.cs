// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the evaluated cost (<see langword="int"/>) of an ability for a specific attribute.
/// </summary>
/// <remarks>
/// <para>By default the resolver reads the ability driving the current graph through the active
/// <see cref="AbilityBehaviorContext"/>. Provide a handle resolver to inspect a different ability.</para>
/// <para>The cost is the evaluated modifier value of the ability's cost effect for the attribute, so a mana cost of
/// 5 resolves as <c>-5</c>. Missing abilities or attributes without a cost resolve to <c>0</c>.</para>
/// </remarks>
/// <param name="attributeKey">The fully qualified key of the attribute to read the cost for.</param>
/// <param name="handleResolver">Optional resolver producing the ability handle to inspect.</param>
public class AbilityCostResolver(
	StringKey attributeKey,
	IObjectResolver<AbilityHandle>? handleResolver = null) : IPropertyResolver
{
	private readonly StringKey _attributeKey = attributeKey;
	private readonly IObjectResolver<AbilityHandle>? _handleResolver = handleResolver;

	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		AbilityHandle? handle = AbilityResolverUtilities.ResolveHandle(graphContext, _handleResolver);

		return new Variant128(handle?.GetCostForAttribute(_attributeKey) ?? 0);
	}
}
