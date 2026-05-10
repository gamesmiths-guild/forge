// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a reference value from either graph variables or shared variables.
/// </summary>
/// <typeparam name="T">The reference type to read.</typeparam>
/// <param name="variableName">The name of the variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class ReferenceVariableResolver<T>(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ReferenceResolver<T>
	where T : class
{
	private readonly StringKey _variableName = variableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	public override T? Resolve(GraphContext graphContext)
	{
		return _scope switch
		{
			VariableScope.Graph => graphContext.GraphVariables.TryGetReference(_variableName, out T? value)
				? value
				: null,
			VariableScope.Shared => graphContext.SharedVariables is not null &&
				graphContext.SharedVariables.TryGetReference(_variableName, out T? value)
					? value
					: null,
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
			_ => throw new ArgumentOutOfRangeException(
				nameof(scope),
				_scope,
				$"Unsupported {nameof(VariableScope)} value."),
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
		};
	}
}
