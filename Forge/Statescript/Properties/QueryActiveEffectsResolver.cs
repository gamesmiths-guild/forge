// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the handles of the active effects on a resolved entity, filtered by an <see cref="EffectQuery"/>.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="AbilityOwnerResolver"/>.</para>
/// <para>Use this to reach active effects the current graph did not apply itself, for example to dispel effects by
/// feeding the result into a RemoveEffect node.</para>
/// <para>An empty query returns every active effect on the entity. To select the applications of one specific effect,
/// set <see cref="EffectQuery.EffectDefinition"/>. For per-element predicates the query cannot express, feed the
/// result into an <see cref="ObjectWhereResolver{T}"/> instead.</para>
/// <para>If the selected entity is not available, the resolver returns an empty array.</para>
/// </remarks>
/// <param name="query">The query the active effects must match. An empty query returns every active effect.</param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
public class QueryActiveEffectsResolver(EffectQuery query, IEntityResolver? entityResolver = null)
	: ObjectArrayResolver<ActiveEffectHandle>
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly EffectQuery _query = query;
	private readonly IEntityResolver _entityResolver = entityResolver ?? _defaultEntityResolver;

	/// <inheritdoc/>
	public override ActiveEffectHandle[] ResolveArray(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null)
		{
			return [];
		}

		return [.. entity.EffectsManager.GetActiveEffects(_query)];
	}
}
