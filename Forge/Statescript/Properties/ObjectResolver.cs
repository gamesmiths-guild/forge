// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Base class for strongly-typed object-backed resolvers.
/// </summary>
/// <typeparam name="T">The value type produced by the resolver.</typeparam>
public abstract class ObjectResolver<T> : IObjectResolver<T>
{
	/// <inheritdoc/>
	[return: MaybeNull]
	public abstract T Resolve(GraphContext graphContext);

	/// <inheritdoc/>
	public Type ValueType => typeof(T);

	object? IObjectResolver.Resolve(GraphContext graphContext)
	{
		return Resolve(graphContext);
	}
}
