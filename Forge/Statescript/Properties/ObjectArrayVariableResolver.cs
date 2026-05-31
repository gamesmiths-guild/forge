// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an object-backed array from either graph variables or shared variables.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="variableName">The name of the array variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class ObjectArrayVariableResolver<T>(
	StringKey variableName,
	VariableScope scope = VariableScope.Graph) : ObjectArrayResolver<T>
{
	private readonly StringKey _variableName = variableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		if (_scope == VariableScope.Graph)
		{
			return graphContext.GraphVariables.TryGetObjectArray(_variableName, out T[]? values)
				? values
				: [];
		}

		if (_scope == VariableScope.Shared)
		{
			return graphContext.SharedVariables is not null &&
				graphContext.SharedVariables.TryGetObjectArray(_variableName, out T[]? values)
					? values
					: [];
		}

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
		throw new ArgumentOutOfRangeException(
			nameof(scope),
			_scope,
			$"Unsupported {nameof(VariableScope)} value.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
	}
}
