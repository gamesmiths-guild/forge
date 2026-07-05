// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the last <see cref="IForgeEntity"/> of a nested entity array resolver. As an <see cref="IEntityResolver"/>
/// it composes with entity-aware resolvers such as <see cref="AttributeResolver"/>.
/// </summary>
/// <param name="source">The resolver providing the source entity array.</param>
public class EntityLastResolver(IObjectArrayResolver<IForgeEntity> source)
	: ObjectLastResolver<IForgeEntity>(source), IEntityResolver
{
}
