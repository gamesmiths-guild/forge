// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Modifiers;
using Attribute = Gamesmiths.Forge.Core.Attribute;

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

	/// <summary>
	/// Creates a custom <see cref="ModifierEvaluatedData"/> with the given parameters.
	/// </summary>
	/// <param name="attribute">The attribute to be modified.</param>
	/// <param name="operation">Which operation to apply.</param>
	/// <param name="magnitude">The magnitude of the modifier.</param>
	/// <param name="channel">The channel of the modifier.</param>
	/// <returns>A new instance of the modifier evalutad data.</returns>
	protected static ModifierEvaluatedData CreateCustomModifierEvaluatedData(
		Attribute attribute,
		ModifierOperation operation,
		float magnitude,
		int channel = 0)
	{
		return new ModifierEvaluatedData(attribute, operation, magnitude, channel);
	}
}
