// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of <see cref="IForgeEntity"/> references by evaluating a nested entity resolver for each element.
/// </summary>
/// <param name="elementResolvers">The nested entity resolvers that produce the array elements.</param>
public class EntityArrayResolver(params IEntityResolver[] elementResolvers)
	: ReferenceArrayCompositeResolver<IForgeEntity>(elementResolvers)
{
}
