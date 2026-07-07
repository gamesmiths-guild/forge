// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the <see cref="IForgeEntity"/> at a given index of a nested entity array resolver. As an
/// <see cref="IEntityResolver"/> it composes with entity-aware resolvers such as <see cref="AttributeResolver"/>.
/// </summary>
/// <param name="source">The resolver providing the source entity array.</param>
/// <param name="index">The resolver providing the zero-based element index. Must resolve to a numeric type.</param>
public class EntityElementAtResolver(IObjectArrayResolver<IForgeEntity> source, IPropertyResolver index)
	: ObjectElementAtResolver<IForgeEntity>(source, index), IEntityResolver
{
}
