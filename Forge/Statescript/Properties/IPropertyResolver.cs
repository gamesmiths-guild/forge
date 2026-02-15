// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Interface for resolving a property value at runtime.
/// </summary>
public interface IPropertyResolver
{
	/// <summary>
	/// Gets the <see cref="Type"/> of the value this resolver produces. Used for compile-time validation when binding
	/// properties to node parameters (e.g., ensuring a timer duration is bound to a numeric property, not a bool).
	/// </summary>
	Type ValueType { get; }

	/// <summary>
	/// Resolves the current value of the property.
	/// </summary>
	/// <param name="graphContext">The graph context providing the runtime state and owner entity.</param>
	/// <returns>The resolved value as a <see cref="Variant128"/>.</returns>
	Variant128 Resolve(GraphContext graphContext);
}
