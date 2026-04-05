// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by looking up another named variable from the graph's runtime <see cref="Variables"/>.
/// This enables property expressions that reference mutable graph variables.
/// </summary>
/// <remarks>
/// This resolver reads the current value of a graph variable by name. If the referenced variable does not exist, the
/// resolver returns a default <see cref="Variant128"/> (zero).
/// </remarks>
/// <param name="referencedVariableName">The name of the graph variable to resolve at runtime.</param>
/// <param name="valueType">The type of the value this resolver produces.</param>
public class VariableResolver(StringKey referencedVariableName, Type valueType) : IPropertyResolver
{
	private readonly StringKey _referencedVariableName = referencedVariableName;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!graphContext.GraphVariables.TryGetVariant(_referencedVariableName, out Variant128 value))
		{
			return default;
		}

		return value;
	}
}
