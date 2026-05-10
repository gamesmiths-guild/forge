// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of reference-typed values at runtime.
/// </summary>
public interface IReferenceArrayResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of each element this resolver produces.
	/// </summary>
	Type ElementType { get; }

	/// <summary>
	/// Resolves the current array value.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved reference array.</returns>
	object?[] ResolveArray(GraphContext graphContext);
}

/// <summary>
/// Resolves a strongly-typed array of reference values at runtime.
/// </summary>
/// <typeparam name="T">The element type produced by the resolver.</typeparam>
public interface IReferenceArrayResolver<out T> : IReferenceArrayResolver
	where T : class
{
	/// <summary>
	/// Resolves the current array value.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved array.</returns>
	new T?[] ResolveArray(GraphContext graphContext);
}
