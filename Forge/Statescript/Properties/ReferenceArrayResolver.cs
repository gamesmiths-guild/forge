// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Base class for strongly-typed reference array resolvers.
/// </summary>
/// <typeparam name="T">The element type produced by the resolver.</typeparam>
public abstract class ReferenceArrayResolver<T> : IReferenceArrayResolver<T>
	where T : class
{
	/// <inheritdoc/>
	public abstract T?[] ResolveArray(GraphContext graphContext);

	/// <inheritdoc/>
	public Type ElementType => typeof(T);

	object?[] IReferenceArrayResolver.ResolveArray(GraphContext graphContext)
	{
		return ResolveArray(graphContext);
	}
}
