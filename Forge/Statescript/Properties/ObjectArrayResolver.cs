// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Base class for strongly-typed object-backed array resolvers.
/// </summary>
/// <typeparam name="T">The element type produced by the resolver.</typeparam>
public abstract class ObjectArrayResolver<T> : IObjectArrayResolver<T>
{
	/// <inheritdoc/>
	public abstract T[] ResolveArray(GraphContext graphContext);

	/// <inheritdoc/>
	public Type ElementType => typeof(T);

	object?[] IObjectArrayResolver.ResolveArray(GraphContext graphContext)
	{
		T[] resolved = ResolveArray(graphContext);
		object?[] values = new object?[resolved.Length];

		for (int i = 0; i < resolved.Length; i++)
		{
			values[i] = resolved[i];
		}

		return values;
	}
}
