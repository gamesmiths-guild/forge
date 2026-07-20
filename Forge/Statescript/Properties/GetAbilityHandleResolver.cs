// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the <see cref="AbilityHandle"/> of a granted ability on a resolved entity, looked up by its
/// <see cref="AbilityData"/>.
/// </summary>
/// <remarks>
/// <para>By default, this resolver targets the owner entity through <see cref="AbilityOwnerResolver"/>.</para>
/// <para>Use this to inspect or drive abilities other than the one running the current graph, for example "the
/// cooldown of my other ability". Feed the result into ability data resolvers or activation nodes.</para>
/// <para>If the selected entity is not available or the ability is not granted, the resolver returns
/// <see langword="null"/>.</para>
/// </remarks>
/// <param name="abilityData">The ability data identifying the granted ability.</param>
/// <param name="entityResolver">The entity resolver that selects which entity to inspect.</param>
/// <param name="sourceResolver">Optional resolver for the granting source entity used to filter the lookup. When
/// omitted (or resolving to <see langword="null"/>), the lookup matches the ability regardless of its granting
/// source.</param>
/// <param name="exactSourceMatch">When <see langword="true"/>, only the instance whose granting source is exactly the
/// resolved source matches, including <see langword="null"/> for abilities granted without a source.</param>
public class GetAbilityHandleResolver(
	AbilityData abilityData,
	IEntityResolver? entityResolver = null,
	IEntityResolver? sourceResolver = null,
	bool exactSourceMatch = false) : ObjectResolver<AbilityHandle>
{
	private static readonly IEntityResolver _defaultEntityResolver = new AbilityOwnerResolver();

	private readonly AbilityData _abilityData = abilityData;
	private readonly IEntityResolver _entityResolver = entityResolver ?? _defaultEntityResolver;
	private readonly IEntityResolver? _sourceResolver = sourceResolver;
	private readonly bool _exactSourceMatch = exactSourceMatch;

	/// <inheritdoc/>
	[return: MaybeNull]
	public override AbilityHandle Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null)
		{
			return null;
		}

		IForgeEntity? source = _sourceResolver?.Resolve(graphContext);

		entity.Abilities.TryGetAbility(_abilityData, out AbilityHandle? handle, source, _exactSourceMatch);

		return handle;
	}
}
