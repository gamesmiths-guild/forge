// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an <see cref="Effect"/> instance from either graph variables or shared variables.
/// </summary>
/// <param name="variableName">The name of the variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class EffectVariableResolver(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ObjectVariableResolver<Effect>(variableName, scope)
{
}
