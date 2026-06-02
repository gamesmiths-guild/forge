// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of object-backed values at runtime.
/// </summary>
public interface IObjectArrayResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of each element this resolver produces.
	/// </summary>
	Type ElementType { get; }

	/// <summary>
	/// Resolves the current array value.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved object-backed array.</returns>
	object?[] ResolveArray(GraphContext graphContext);
}

/// <summary>
/// Resolves a strongly-typed array of object-backed values at runtime.
/// </summary>
/// <typeparam name="T">The element type produced by the resolver.</typeparam>
public interface IObjectArrayResolver<out T> : IObjectArrayResolver
{
	/// <summary>
	/// Resolves the current array value.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved array.</returns>
	[return: NotNull]
	new T[] ResolveArray(GraphContext graphContext);
}
