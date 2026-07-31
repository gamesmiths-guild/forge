// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the handles of the active effects on a resolved entity, optionally filtered by an <see cref="EffectData"/>
/// or by a full <see cref="EffectQuery"/>.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="AbilityOwnerResolver"/>.</para>
/// <para>Use this to reach active effects the current graph did not apply itself, for example to dispel effects by
/// feeding the result into a RemoveEffect node.</para>
/// <para>Filtering by an <see cref="EffectQuery"/> covers the common dispel-by-category case without a Where lambda.
/// For per-element predicates the query cannot express, feed the unfiltered result into an
/// <see cref="ObjectWhereResolver{T}"/> instead.</para>
/// <para>If the selected entity is not available, the resolver returns an empty array.</para>
/// </remarks>
public class QueryActiveEffectsResolver : ObjectArrayResolver<ActiveEffectHandle>
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly EffectData? _effectData;
	private readonly EffectQuery? _query;
	private readonly IEntityResolver _entityResolver;

	/// <summary>
	/// Initializes a new instance of the <see cref="QueryActiveEffectsResolver"/> class filtering by effect data.
	/// </summary>
	/// <param name="effectData">The effect data to filter by, or <see langword="null"/> to return every active effect.
	/// </param>
	/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
	public QueryActiveEffectsResolver(EffectData? effectData, IEntityResolver? entityResolver = null)
	{
		_effectData = effectData;
		_entityResolver = entityResolver ?? _defaultEntityResolver;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="QueryActiveEffectsResolver"/> class filtering by an effect query.
	/// </summary>
	/// <param name="query">The query the active effects must match. An empty query returns every active effect.
	/// </param>
	/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
	public QueryActiveEffectsResolver(EffectQuery query, IEntityResolver? entityResolver = null)
	{
		_query = query;
		_entityResolver = entityResolver ?? _defaultEntityResolver;
	}

	/// <inheritdoc/>
	public override ActiveEffectHandle[] ResolveArray(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null)
		{
			return [];
		}

		IEnumerable<ActiveEffectHandle> handles = GetHandles(entity.EffectsManager);

		return [.. handles];
	}

	private IEnumerable<ActiveEffectHandle> GetHandles(EffectsManager effectsManager)
	{
		if (_query.HasValue)
		{
			return effectsManager.GetActiveEffects(_query.Value);
		}

		return _effectData.HasValue
			? effectsManager.GetActiveEffects(_effectData.Value)
			: effectsManager.GetActiveEffects();
	}
}
