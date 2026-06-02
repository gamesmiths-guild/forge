// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an object-backed value from either graph variables or shared variables.
/// </summary>
/// <typeparam name="T">The value type to read.</typeparam>
/// <param name="variableName">The name of the variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class ObjectVariableResolver<T>(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ObjectResolver<T>
{
	private readonly StringKey _variableName = variableName;
	private readonly VariableScope _scope = scope;

	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		if (_scope == VariableScope.Graph)
		{
			return graphContext.GraphVariables.TryGetObject(_variableName, typeof(T), out object? value)
				? (T)value!
				: default!;
		}

		if (_scope == VariableScope.Shared)
		{
			return graphContext.SharedVariables is not null &&
				graphContext.SharedVariables.TryGetObject(_variableName, typeof(T), out object? value)
					? (T)value!
					: default!;
		}

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
		throw new ArgumentOutOfRangeException(
			nameof(scope),
			_scope,
			$"Unsupported {nameof(VariableScope)} value.");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
	}
}
