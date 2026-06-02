// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an object-backed value at runtime.
/// </summary>
public interface IObjectResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of the value this resolver produces.
	/// </summary>
	Type ValueType { get; }

	/// <summary>
	/// Resolves the current value of the property.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved object-backed value.</returns>
	object? Resolve(GraphContext graphContext);
}

/// <summary>
/// Resolves a strongly-typed object-backed value at runtime.
/// </summary>
/// <typeparam name="T">The value type produced by the resolver.</typeparam>
public interface IObjectResolver<out T> : IObjectResolver
{
	/// <summary>
	/// Resolves the current value of the property.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved value.</returns>
	[return: MaybeNull]
	new T Resolve(GraphContext graphContext);
}
