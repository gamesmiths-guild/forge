// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Represents the definition of a graph property, including its name and the resolver used to compute its value at
/// runtime. This is immutable definition data that belongs to the graph.
/// </summary>
/// <param name="Name">The name of the property, used as the lookup key at runtime.</param>
/// <param name="Resolver">The resolver used to provide the property's value at runtime.</param>
public readonly record struct PropertyDefinition(StringKey Name, IPropertyResolver Resolver);
