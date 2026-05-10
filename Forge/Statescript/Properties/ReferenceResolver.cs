// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Base class for strongly-typed reference resolvers.
/// </summary>
/// <typeparam name="T">The reference type produced by the resolver.</typeparam>
public abstract class ReferenceResolver<T> : IReferenceResolver<T>
	where T : class
{
	/// <inheritdoc/>
	public abstract T? Resolve(GraphContext graphContext);

	/// <inheritdoc/>
	public Type ValueType => typeof(T);

	object? IReferenceResolver.Resolve(GraphContext graphContext)
	{
		return Resolve(graphContext);
	}
}
