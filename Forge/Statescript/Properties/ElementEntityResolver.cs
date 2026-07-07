// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the <see cref="IForgeEntity"/> array element currently being iterated by an enclosing array resolver. As an
/// <see cref="IEntityResolver"/> it composes with entity-aware resolvers such as <see cref="AttributeResolver"/> and
/// <see cref="TagQueryResolver"/>, enabling per-element predicates and sort keys (e.g. "the current entity's health").
/// </summary>
public class ElementEntityResolver : ElementResolver<IForgeEntity>, IEntityResolver;
