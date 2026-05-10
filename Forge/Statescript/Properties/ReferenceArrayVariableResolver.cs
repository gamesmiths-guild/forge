// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a reference array from either graph variables or shared variables.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="variableName">The name of the array variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class ReferenceArrayVariableResolver<T>(
	StringKey variableName,
	VariableScope scope = VariableScope.Graph) : ReferenceArrayResolver<T>
	where T : class
{
	private readonly StringKey _variableName = variableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	public override T?[] ResolveArray(GraphContext graphContext)
	{
		return _scope switch
		{
			VariableScope.Graph => graphContext.GraphVariables.TryGetReferenceArray(_variableName, out T?[]? values)
				? values ?? []
				: [],
			VariableScope.Shared => graphContext.SharedVariables is not null &&
				graphContext.SharedVariables.TryGetReferenceArray(_variableName, out T?[]? values)
					? values ?? []
					: [],
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
			_ => throw new ArgumentOutOfRangeException(
				nameof(scope),
				_scope,
				$"Unsupported {nameof(VariableScope)} value."),
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
		};
	}
}
