// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a property value by looking up another named variable from either the graph or shared runtime
/// <see cref="Variables"/> bag.
/// </summary>
/// <remarks>
/// This resolver reads the current value of a mutable variable by name. Use <see cref="VariableScope.Graph"/> for
/// per-graph-instance state and <see cref="VariableScope.Shared"/> for entity-level shared state. If the referenced
/// variable does not exist, the resolver returns a default <see cref="Variant128"/> (zero).
/// </remarks>
/// <param name="referencedVariableName">The name of the variable to resolve at runtime.</param>
/// <param name="valueType">The type of the value this resolver produces.</param>
/// <param name="scope">Which variable bag to read from. Defaults to <see cref="VariableScope.Graph"/>.</param>
public class VariableResolver(
	StringKey referencedVariableName,
	Type valueType,
	VariableScope scope = VariableScope.Graph) : IPropertyResolver
{
	private readonly StringKey _referencedVariableName = referencedVariableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!TryGetVariables(graphContext, out Variables? variables))
		{
			return default;
		}

		if (!variables.TryGetVariant(_referencedVariableName, out Variant128 value))
		{
			return default;
		}

		return value;
	}

	private bool TryGetVariables(
		GraphContext graphContext,
		[NotNullWhen(true)] out Variables? variables)
	{
		switch (_scope)
		{
			case VariableScope.Graph:
				variables = graphContext.GraphVariables;
				return true;

			case VariableScope.Shared:
				variables = graphContext.SharedVariables;
				return variables is not null;

			default:
				throw new InvalidOperationException($"Unsupported {nameof(VariableScope)} value: {_scope}.");
		}
	}
}
