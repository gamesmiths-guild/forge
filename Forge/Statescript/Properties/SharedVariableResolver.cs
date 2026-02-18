// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading a named variable from the graph context's
/// <see cref="GraphContext.SharedVariables"/>. When a graph is driven by an ability, these are the owner entity's
/// <see cref="IForgeEntity.SharedVariables"/>, enabling cross-ability communication (e.g., an "ability lock" flag
/// shared by all abilities on a hero).
/// </summary>
/// <remarks>
/// <para>If the graph context has no shared variables or the shared variables do not contain the specified name, the
/// resolver returns a default <see cref="Variant128"/> (zero).</para>
/// <para>Unlike <see cref="VariableResolver"/> which reads from per-graph-instance variables, this resolver reads from
/// the shared bag, allowing one ability's graph to read values written by another.</para>
/// </remarks>
/// <param name="variableName">The name of the shared variable to read.</param>
/// <param name="valueType">The type of the value this resolver produces.</param>
public class SharedVariableResolver(StringKey variableName, Type valueType) : IPropertyResolver
{
	private readonly StringKey _variableName = variableName;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (graphContext.SharedVariables is null)
		{
			return default;
		}

		if (!graphContext.SharedVariables.TryGetVariant(_variableName, out Variant128 value))
		{
			return default;
		}

		return value;
	}
}
