// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an <see cref="IForgeEntity"/> reference from either graph variables or shared variables.
/// </summary>
/// <param name="variableName">The name of the variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class EntityVariableResolver(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ReferenceVariableResolver<IForgeEntity>(variableName, scope), IEntityResolver
{
}
