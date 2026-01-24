// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// Provides a magnitude for the <see cref="Modifiers.Modifier"/> in a <see cref="Effect"/>.
/// </summary>
public readonly record struct ModifierMagnitude
{
	/// <summary>
	/// Gets the type of magnitude calculation being used for this magnitude.
	/// </summary>
	public readonly MagnitudeCalculationType MagnitudeCalculationType { get; }

	/// <summary>
	/// Gets the <see cref="ScalableFloat"/> used for calculating this magnitude.
	/// </summary>
	/// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> == <see cref="MagnitudeCalculationType.ScalableFloat"/>.
	/// </remarks>
	public readonly ScalableFloat? ScalableFloatMagnitude { get; }

	/// <summary>
	/// Gets the <see cref="AttributeBasedFloat"/> used for calculating this magnitude.
	/// </summary>
	/// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> ==
	/// <see cref="MagnitudeCalculationType.AttributeBased"/>.
	/// </remarks>
	public readonly AttributeBasedFloat? AttributeBasedFloat { get; }

	/// <summary>
	/// Gets the <see cref="CustomCalculationBasedFloat"/> used for calculating this magnitude.
	/// </summary>
	/// /// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> ==
	/// <see cref="MagnitudeCalculationType.CustomCalculatorClass"/>.
	/// </remarks>
	public readonly CustomCalculationBasedFloat? CustomCalculationBasedFloat { get; }

	/// <summary>
	/// Gets the <see cref="SetByCallerFloat"/> used for calculating this magnitude.
	/// </summary>
	/// /// <remarks>
	/// Is only valid if <see cref="MagnitudeCalculationType"/> ==
	/// <see cref="MagnitudeCalculationType.SetByCaller"/>.
	/// </remarks>
	public readonly SetByCallerFloat? SetByCallerFloat { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ModifierMagnitude"/> struct.
	/// </summary>
	/// <param name="magnitudeCalculationType">The type of calculation used for this magnitude.</param>
	/// <param name="scalableFloatMagnitude">The scalable float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.ScalableFloat"/>.</param>
	/// <param name="attributeBasedFloat">The attribute based float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.AttributeBased"/>.</param>
	/// <param name="customCalculationBasedFloat">The custom calculation based float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.CustomCalculatorClass"/>.</param>
	/// <param name="setByCallerFloat">The set by caller float used for this magnitude, if set as
	/// <see cref="MagnitudeCalculationType.SetByCaller"/>.</param>
	public ModifierMagnitude(
		MagnitudeCalculationType magnitudeCalculationType,
		ScalableFloat? scalableFloatMagnitude = null,
		AttributeBasedFloat? attributeBasedFloat = null,
		CustomCalculationBasedFloat? customCalculationBasedFloat = null,
		SetByCallerFloat? setByCallerFloat = null)
	{
		Validation.Assert(
			(magnitudeCalculationType == MagnitudeCalculationType.ScalableFloat && scalableFloatMagnitude.HasValue
					&& !attributeBasedFloat.HasValue && !customCalculationBasedFloat.HasValue
					&& !setByCallerFloat.HasValue)
				|| (magnitudeCalculationType == MagnitudeCalculationType.AttributeBased && attributeBasedFloat.HasValue
					&& !scalableFloatMagnitude.HasValue && !customCalculationBasedFloat.HasValue
					&& !setByCallerFloat.HasValue)
				|| (magnitudeCalculationType == MagnitudeCalculationType.CustomCalculatorClass
					&& customCalculationBasedFloat.HasValue && !scalableFloatMagnitude.HasValue
					&& !attributeBasedFloat.HasValue && !setByCallerFloat.HasValue)
				|| (magnitudeCalculationType == MagnitudeCalculationType.SetByCaller && setByCallerFloat.HasValue
					&& !attributeBasedFloat.HasValue && !scalableFloatMagnitude.HasValue
					&& !customCalculationBasedFloat.HasValue),
			$"Invalid parameters for {nameof(magnitudeCalculationType)} == {magnitudeCalculationType}.");

		MagnitudeCalculationType = magnitudeCalculationType;
		ScalableFloatMagnitude = scalableFloatMagnitude;
		AttributeBasedFloat = attributeBasedFloat;
		CustomCalculationBasedFloat = customCalculationBasedFloat;
		SetByCallerFloat = setByCallerFloat;
	}

	internal readonly float GetMagnitude(
		Effect effect,
		IForgeEntity target,
		int level,
		EffectEvaluatedData? effectEvaluatedData = null)
	{
		switch (MagnitudeCalculationType)
		{
			case MagnitudeCalculationType.ScalableFloat:
				Validation.Assert(
					ScalableFloatMagnitude.HasValue,
					$"{nameof(ScalableFloatMagnitude)} should always have a value at this point.");
				return ScalableFloatMagnitude.Value.GetValue(level);

			case MagnitudeCalculationType.AttributeBased:
				Validation.Assert(
					AttributeBasedFloat.HasValue,
					$"{nameof(AttributeBasedFloat)} should always have a value at this point.");
				return AttributeBasedFloat.Value.CalculateMagnitude(
					effect, target, level, effectEvaluatedData?.SnapshotAttributes);

			case MagnitudeCalculationType.CustomCalculatorClass:
				Validation.Assert(
					CustomCalculationBasedFloat.HasValue,
					$"{nameof(CustomCalculationBasedFloat)} should always have a value at this point.");
				return CustomCalculationBasedFloat.Value.CalculateMagnitude(effect, target, level, effectEvaluatedData);

			case MagnitudeCalculationType.SetByCaller:
				Validation.Assert(
					SetByCallerFloat.HasValue,
					$"{nameof(SetByCallerFloat)} should always have a value at this point.");

				if (SetByCallerFloat.Value.Snapshot && effectEvaluatedData is not null)
				{
					if (effectEvaluatedData.SnapshotSetByCallers.TryGetValue(
						SetByCallerFloat.Value.Tag, out var snapshotValue))
					{
						return snapshotValue;
					}

					effectEvaluatedData.SnapshotSetByCallers.Add(
						SetByCallerFloat.Value.Tag, effect.DataTag[SetByCallerFloat.Value.Tag]);
				}

				return effect.DataTag[SetByCallerFloat.Value.Tag];

			default:
				return 0;
		}
	}
}
