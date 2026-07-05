// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the first <see cref="IForgeEntity"/> of a nested entity array resolver. As an <see cref="IEntityResolver"/>
/// it composes with entity-aware resolvers such as <see cref="AttributeResolver"/>.
/// </summary>
/// <param name="source">The resolver providing the source entity array.</param>
public class EntityFirstResolver(IObjectArrayResolver<IForgeEntity> source)
	: ObjectFirstResolver<IForgeEntity>(source), IEntityResolver
{
}
