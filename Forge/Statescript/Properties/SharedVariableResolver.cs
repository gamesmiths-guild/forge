// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by reading a named variable from the graph owner entity's
/// <see cref="IForgeEntity.SharedVariables"/>. Shared variables are accessible by all graph instances running on the
/// same entity, enabling cross-ability communication (e.g., an "ability lock" flag shared by all abilities on a hero).
/// </summary>
/// <remarks>
/// <para>If the graph context has no owner or the owner's shared variables do not contain the specified name, the
/// resolver returns a default <see cref="Variant128"/> (zero).</para>
/// <para>Unlike <see cref="VariableResolver"/> which reads from per-graph-instance variables, this resolver reads from
/// the entity-level shared bag, allowing one ability's graph to read values written by another.</para>
/// </remarks>
/// <param name="variableName">The name of the shared variable to read from the owner entity.</param>
/// <param name="valueType">The type of the value this resolver produces.</param>
public class SharedVariableResolver(StringKey variableName, Type valueType) : IPropertyResolver
{
	private readonly StringKey _variableName = variableName;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (graphContext.Owner is null)
		{
			return default;
		}

		if (!graphContext.Owner.SharedVariables.TryGetVariant(_variableName, graphContext, out Variant128 value))
		{
			return default;
		}

		return value;
	}
}
