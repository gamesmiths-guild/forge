// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Calculator;

/// <summary>
/// Custom ExecutionCalculators are useful when you have to modify more than one attribute with the same logic.
/// </summary>
public abstract class CustomExecution : CustomCalculator
{
	/// <summary>
	/// Calculates the execution and returns the calculated modifiers for each modified attribute.
	/// </summary>
	/// <param name="effect">The effect to be used as context for the calculation.</param>
	/// <param name="target">The target entity to be used as context for the calculation.</param>
	/// <returns>An array of evaluated datas for each modified attribute.</returns>
	public abstract ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target);
}
