// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Interface for resolving an array property value at runtime.
/// </summary>
public interface IArrayPropertyResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of the array element this resolver produces. Used for compile-time validation when binding
	/// array properties to node parameters (e.g., ensuring a node expecting int[] is not bound to a float[] property).
	/// </summary>
	Type ElementType { get; }

	/// <summary>
	/// Resolves the current value of the property as an array.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state and owner entity.</param>
	/// <returns>The resolved value as an array of <see cref="Variant128"/>.</returns>
	Variant128[] ResolveArray(GraphContext graphContext);
}
