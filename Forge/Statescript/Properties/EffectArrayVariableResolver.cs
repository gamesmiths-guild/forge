// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of <see cref="Effect"/> instances from either graph variables or shared variables.
/// </summary>
/// <param name="variableName">The name of the array variable to read.</param>
/// <param name="scope">Which variable bag to read from.</param>
public class EffectArrayVariableResolver(StringKey variableName, VariableScope scope = VariableScope.Graph)
	: ObjectArrayVariableResolver<Effect>(variableName, scope)
{
}
