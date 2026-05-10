// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a reference-typed value at runtime.
/// </summary>
public interface IReferenceResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of the value this resolver produces.
	/// </summary>
	Type ValueType { get; }

	/// <summary>
	/// Resolves the current value of the property.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved reference value.</returns>
	object? Resolve(GraphContext graphContext);
}

/// <summary>
/// Resolves a strongly-typed reference value at runtime.
/// </summary>
/// <typeparam name="T">The reference type produced by the resolver.</typeparam>
public interface IReferenceResolver<out T> : IReferenceResolver
	where T : class
{
	/// <summary>
	/// Resolves the current value of the property.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state.</param>
	/// <returns>The resolved value.</returns>
	new T? Resolve(GraphContext graphContext);
}
