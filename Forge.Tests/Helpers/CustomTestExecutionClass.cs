// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;

using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.Tests.Helpers;

public class CustomTestExecutionClass : CustomExecution
{
	private int _internalCount;

	public AttributeCaptureDefinition SourceAttribute1 { get; }

	public AttributeCaptureDefinition SourceAttribute2 { get; }

	public AttributeCaptureDefinition SourceAttribute3 { get; }

	public AttributeCaptureDefinition TargetAttribute1 { get; }

	public AttributeCaptureDefinition TargetAttribute2 { get; }

	public CustomTestExecutionClass()
	{
		SourceAttribute1 = new AttributeCaptureDefinition(
			"TestAttributeSet.Attribute3",
			AttributeCaptureSource.Source,
			false);
		SourceAttribute2 = new AttributeCaptureDefinition(
			"TestAttributeSet.Attribute5",
			AttributeCaptureSource.Source,
			false);
		SourceAttribute3 = new AttributeCaptureDefinition(
			"TestAttributeSet.Attribute90",
			AttributeCaptureSource.Source,
			true);
		TargetAttribute1 = new AttributeCaptureDefinition(
			"TestAttributeSet.Attribute1",
			AttributeCaptureSource.Target,
			false);
		TargetAttribute2 = new AttributeCaptureDefinition(
			"TestAttributeSet.Attribute2",
			AttributeCaptureSource.Target,
			false);

		AttributesToCapture.Add(SourceAttribute1);
		AttributesToCapture.Add(SourceAttribute2);
		AttributesToCapture.Add(SourceAttribute3);
		AttributesToCapture.Add(TargetAttribute1);
		AttributesToCapture.Add(TargetAttribute2);

		CustomCueParameters.Add("custom.parameter", 0);
	}

	public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target)
	{
		var result = new List<ModifierEvaluatedData>();

		var sourceAttribute1value = CaptureAttributeMagnitude(SourceAttribute1, effect, effect.Ownership.Source);
		var sourceAttribute2value = CaptureAttributeMagnitude(SourceAttribute2, effect, effect.Ownership.Source);

		if (TargetAttribute1.TryGetAttribute(target, out Attribute? targetAttribute1))
		{
			result.Add(CreateCustomModifierEvaluatedData(
				targetAttribute1,
				ModifierOperation.FlatBonus,
				sourceAttribute1value * sourceAttribute2value));
		}

		if (TargetAttribute2.TryGetAttribute(target, out Attribute? targetAttribute2))
		{
			result.Add(CreateCustomModifierEvaluatedData(
				targetAttribute2,
				ModifierOperation.FlatBonus,
				sourceAttribute1value + sourceAttribute2value));
		}

		if (SourceAttribute3.TryGetAttribute(effect.Ownership.Source, out Attribute? sourceAttribute3))
		{
			result.Add(CreateCustomModifierEvaluatedData(
				sourceAttribute3,
				ModifierOperation.FlatBonus,
				-1));
		}

		_internalCount++;
		CustomCueParameters["custom.parameter"] = _internalCount * (sourceAttribute1value + sourceAttribute2value);

		return [.. result];
	}
}
