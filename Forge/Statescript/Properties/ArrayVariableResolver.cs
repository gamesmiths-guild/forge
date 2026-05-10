// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array variable from either the graph or shared runtime <see cref="Variables"/> bag.
/// </summary>
/// <remarks>
/// Use <see cref="VariableScope.Graph"/> for per-graph-instance array variables and <see cref="VariableScope.Shared"/>
/// for entity-level shared arrays. If the referenced array does not exist, this resolver returns an empty array.
/// </remarks>
/// <param name="variableName">The name of the array variable to read.</param>
/// <param name="elementType">The type of each element in the array.</param>
/// <param name="scope">Which variable bag to read from. Defaults to <see cref="VariableScope.Graph"/>.</param>
public class ArrayVariableResolver(
	StringKey variableName,
	Type elementType,
	VariableScope scope = VariableScope.Graph) : IArrayPropertyResolver
{
	private readonly StringKey _variableName = variableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	public Type ElementType { get; } = elementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		if (!TryGetVariables(graphContext, out Variables? variables))
		{
			return [];
		}

		int length = variables.GetArrayLength(_variableName);
		if (length < 0)
		{
			return [];
		}

		var values = new Variant128[length];
		for (int i = 0; i < length; i++)
		{
			_ = variables.TryGetArrayVariant(_variableName, i, out values[i]);
		}

		return values;
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
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
				throw new ArgumentOutOfRangeException(
					nameof(scope),
					_scope,
					$"Unsupported {nameof(VariableScope)} value.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
		}
	}
}
