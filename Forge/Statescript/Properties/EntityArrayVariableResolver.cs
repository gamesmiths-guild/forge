// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of <see cref="IForgeEntity"/> references from either graph variables or shared variables.
/// </summary>
/// <param name="variableName">The name of the array variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class EntityArrayVariableResolver(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ReferenceArrayVariableResolver<IForgeEntity>(variableName, scope)
{
}
