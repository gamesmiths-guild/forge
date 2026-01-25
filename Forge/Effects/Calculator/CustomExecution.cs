// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

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
	/// <param name="effectEvaluatedData">The evaluated data for the effect.</param>
	/// <returns>An array of evaluated data for each modified attribute.</returns>
	public abstract ModifierEvaluatedData[] EvaluateExecution(
		Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData);

	internal static bool ExecutionHasInvalidAttributeCaptures(
		CustomExecution execution,
		Effect effect,
		IForgeEntity target)
	{
		foreach (AttributeCaptureDefinition capturedAttribute in execution.AttributesToCapture)
		{
			switch (capturedAttribute.Source)
			{
				case AttributeCaptureSource.Target:

					if (!target.Attributes.ContainsAttribute(capturedAttribute.Attribute))
					{
						return true;
					}

					break;

				case AttributeCaptureSource.Source:

					IForgeEntity? sourceEntity = effect.Ownership.Source;

					if (sourceEntity?.Attributes.ContainsAttribute(capturedAttribute.Attribute) != true)
					{
						return true;
					}

					break;
			}
		}

		return false;
	}
}
