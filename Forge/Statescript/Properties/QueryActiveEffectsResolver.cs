// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the handles of the active effects on a resolved entity, optionally filtered by an <see cref="EffectData"/>.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="AbilityOwnerResolver"/>.</para>
/// <para>Use this to reach active effects the current graph did not apply itself, for example to dispel effects by
/// feeding the result into a RemoveEffect node.</para>
/// <para>If the selected entity is not available, the resolver returns an empty array.</para>
/// </remarks>
/// <param name="effectData">The effect data to filter by, or <see langword="null"/> to return every active effect.
/// </param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
public class QueryActiveEffectsResolver(
	EffectData? effectData,
	IEntityResolver? entityResolver = null) : ObjectArrayResolver<ActiveEffectHandle>
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly EffectData? _effectData = effectData;
	private readonly IEntityResolver _entityResolver = entityResolver ?? _defaultEntityResolver;

	/// <inheritdoc/>
	public override ActiveEffectHandle[] ResolveArray(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null)
		{
			return [];
		}

		IEnumerable<ActiveEffectHandle> handles = _effectData.HasValue
			? entity.EffectsManager.GetActiveEffects(_effectData.Value)
			: entity.EffectsManager.GetActiveEffects();

		return [.. handles];
	}
}
