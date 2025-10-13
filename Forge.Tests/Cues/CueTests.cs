// Copyright © Gamesmiths Guild.
#pragma warning disable SA1118 // ParameterMustNotSpanMultipleLines

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.TagsAndCuesFixture;
using static Gamesmiths.Forge.Tests.Helpers.TagsAndCuesFixture.TestCue;

namespace Gamesmiths.Forge.Tests.Cues;

public class CueTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private enum TestCueExecutionType
	{
		Execution = 0,
		Application = 1,
		Update = 2,
	}

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;
	private readonly TestCue[] _testCues = tagsAndCuesFixture.TestCueInstances;

	[Theory]
	[Trait("Execute", null)]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 6, 0.6f, false },
			new object[] { 1, 2, 2, 0.4f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, false },
			new object[] { 1, 1, 1, 0.2f, false },
			new object[] { 2, 1, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 6, 0.6f, false },
			new object[] { 1, 2, 2, 0.4f, false },
			new object[] { 2, 2, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
			new object[] { "TestAttributeSet.Attribute1", 2f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 5, 0.5f, false },
			new object[] { 1, 1, 1, 0.2f, false },
			new object[] { 2, 1, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 10, 1f, false },
			new object[] { 1, 2, 2, 0.4f, false },
			new object[] { 2, 2, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 99f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute1" },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeModifier, "TestAttributeSet.Attribute1" },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, false },
			new object[] { 1, 1, 99, 1f, false },
			new object[] { 2, 1, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 0, 0f, false },
			new object[] { 1, 2, 99, 1f, false },
			new object[] { 2, 2, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 99f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute1" },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeModifier, "TestAttributeSet.Attribute1" },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, false },
			new object[] { 1, 1, 99, 1f, false },
			new object[] { 2, 1, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, false },
			new object[] { 1, 1, 99, 1f, false },
			new object[] { 2, 1, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
			new object[] { "TestAttributeSet.Attribute2", 2f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 2, 2, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, false },
			new object[] { 1, 4, 4, 0.4f, false },
		})]
	[InlineData(
		new object[] { },
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute3" },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 0, 0f, false },
			new object[] { 1, 2, 2, 0.4f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
			new object[] { "TestAttributeSet.Attribute1", 2f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
			new object[] { 2, 1, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 0, 0f, false },
			new object[] { 1, 2, 2, 0.4f, false },
			new object[] { 2, 2, 0, 0f, false },
		})]
	[InlineData(
		new object[] { },
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute2" },
			new object[] { 2, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute3" },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
			new object[] { "Invalid.Attribute", 2f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 2, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, false },
			new object[] { 1, 4, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "Invalid.Attribute", 3f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 3f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeBaseValue, "TestAttributeSet.Attribute1" },
		},
		new object[]
		{
			new object[] { 0, 1, 4, 0.4f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 10, 1f, false },
		})]
	public void Instant_effect_triggers_execute_cues_with_expected_results(
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] cueTestDatas1,
		object[] cueTestDatas2)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInstantEffectData(
			CreateModifiers(modifiersData),
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(cueDatas));
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ResetCues();
		entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Execution, cueTestDatas1);

		effect.LevelUp();
		entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Execution, cueTestDatas2);
	}

	[Theory]
	[Trait("Apply", null)]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 5f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 5, 0.5f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 5, 0.5f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 5f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute2" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		})]
	[InlineData(
		new object[] { },
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		})]
	[InlineData(
		new object[] { },
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 5f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "Invalid.Attribute", 5f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 20f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 100, CueMagnitudeType.AttributeModifier, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 20, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 20, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 20f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 100, CueMagnitudeType.AttributeOverflow, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 11, 0.11f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 11, 0.11f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 20f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 100, CueMagnitudeType.AttributeValidModifier, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 9, 0.09f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 9, 0.09f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute5", 20f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 100, CueMagnitudeType.AttributeMin, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute5", 20f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 100, CueMagnitudeType.AttributeMax, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 99, 0.99f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 99, 0.99f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute5", 20f },
		},
		true,
		new object[]
		{
			new object[]
			{
				0,
				0,
				100,
				CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel,
				"TestAttributeSet.Attribute90",
			},
		},
		new object[]
		{
			new object[] { 0, 1, 90, 0.9f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 90, 0.9f, false },
		})]
	public void Infinite_effect_triggers_apply_and_remove_cues_with_expected_results(
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] cueTestDatas1,
		object[] cueTestDatas2)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInfiniteEffectData(
			CreateModifiers(modifiersData),
			true,
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(cueDatas));
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ResetCues();
		ActiveEffectHandle? activeEffectHandler = entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Application, cueTestDatas1);

		entity.EffectsManager.UnapplyEffect(activeEffectHandler!);
		TestCueExecutionData(TestCueExecutionType.Application, cueTestDatas2);
	}

	[Theory]
	[Trait("Apply and Execute", null)]
	[InlineData(
		10f,
		1f,
		true,
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		false,
		5f,
		5f,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 6, 1, 0.1f, true },
			new object[] { 1, 6, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 11, 1, 0.1f, false },
			new object[] { 1, 11, 1, 0.2f, false },
		},
		true,
		true,
		true)]
	[InlineData(
		10f,
		1f,
		false,
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
			new object[] { "TestAttributeSet.Attribute2", 2f },
		},
		false,
		5f,
		6f,
		new object[]
		{
			new object[] { 0, 1, 11, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 100, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.02f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.02f, true },
		},
		new object[]
		{
			new object[] { 0, 5, 1, 0f, true },
			new object[] { 1, 5, 12, 0.12f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 2, 0.02f, false },
		},
		new object[]
		{
			new object[] { 0, 10, 1, 0f, false },
			new object[] { 1, 10, 22, 0.22f, false },
		},
		true,
		false,
		true)]
	[InlineData(
		10f,
		1f,
		true,
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
			new object[] { "TestAttributeSet.Attribute2", 2f },
		},
		true,
		5f,
		6f,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 2, 102, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 4, 0.02f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 6, 1, 0.1f, false },
			new object[] { 1, 6, 14, 0.12f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 11, 1, 0.1f, false },
			new object[] { 1, 11, 24, 0.22f, false },
		},
		false,
		true,
		false)]
	[InlineData(
		10f,
		1f,
		false,
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
			new object[] { "TestAttributeSet.Attribute2", 2f },
		},
		true,
		5f,
		6f,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 2, 102, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 5, 1, 0.1f, false },
			new object[] { 1, 5, 12, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 10, 1, 0.1f, false },
			new object[] { 1, 10, 22, 0.2f, false },
		},
		false,
		false,
		false)]
	[InlineData(
		10f,
		1f,
		true,
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		false,
		5f,
		5f,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 6, 0, 0f, true },
			new object[] { 1, 6, 1, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 11, 0, 0f, false },
			new object[] { 1, 11, 1, 0.2f, false },
		},
		true,
		true,
		true)]
	public void Periodic_effect_triggers_apply_remove_and_execute_cues_with_expected_results(
		float duration,
		float period,
		bool executeOnApplication,
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		float firstDeltaUpdate,
		float secondDeltaUpdate,
		object[] cueDatas,
		object[] applicationCueTestDatas1,
		object[] executionCueTestDatas1,
		object[] applicationCueTestDatas2,
		object[] executionCueTestDatas2,
		object[] applicationCueTestDatas3,
		object[] executionCueTestDatas3,
		bool applyTriggered,
		bool executeTriggered,
		bool isFiredInCorrectOrder)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateDurationPeriodicEffectData(
			duration,
			period,
			executeOnApplication,
			CreateModifiers(modifiersData),
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(cueDatas));
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		var firstTriggered = new ManualResetEventSlim(false);
		var secondTriggered = new ManualResetEventSlim(false);
		var isOrderCorrect = false;

		ResetCues();
		_testCues[0].OnApplied += () =>
		{
			firstTriggered.Set();

			// Ensure the second event hasn't fired yet
			isOrderCorrect = !secondTriggered.IsSet;
		};

		_testCues[0].OnExecuted += secondTriggered.Set;

		entity.EffectsManager.ApplyEffect(effect);
		firstTriggered.IsSet.Should().Be(applyTriggered);
		secondTriggered.IsSet.Should().Be(executeTriggered);
		isOrderCorrect.Should().Be(isFiredInCorrectOrder);

		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas1);
		TestCueExecutionData(TestCueExecutionType.Execution, executionCueTestDatas1);

		entity.EffectsManager.UpdateEffects(firstDeltaUpdate);

		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas2);
		TestCueExecutionData(TestCueExecutionType.Execution, executionCueTestDatas2);

		entity.EffectsManager.UpdateEffects(secondDeltaUpdate);

		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas3);
		TestCueExecutionData(TestCueExecutionType.Execution, executionCueTestDatas3);
	}

	[Theory]
	[Trait("Update", null)]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 2, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		false,
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 2, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		true,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 99f },
		},
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 2, 0.2f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 99f },
		},
		false,
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, false },
			new object[] { 1, 1, 2, 0.2f, false },
		})]
	public void Infinite_effect_triggers_update_cues_when_level_up_with_expected_results(
		object[] modifiersData,
		bool snapshotLevel,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] applicationCueTestDatas1,
		object[] updateCueTestDatas1,
		object[] applicationCueTestDatas2,
		object[] updateCueTestDatas2,
		object[] applicationCueTestDatas3,
		object[] updateCueTestDatas3)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInfiniteEffectData(
			CreateModifiers(modifiersData),
			snapshotLevel,
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(cueDatas));
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ResetCues();
		ActiveEffectHandle? activeEffectHandler = entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas1);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas1);

		effect.LevelUp();
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas2);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas2);

		entity.EffectsManager.UnapplyEffect(activeEffectHandler!);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas3);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas3);
	}

	[Theory]
	[Trait("Update", null)]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", false, 1f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute2", 1f },
			new object[] { "TestAttributeSet.Attribute2", 1f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 4, 0.4f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 2, -2, 0f, true },
			new object[] { 1, 2, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", true, 1f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute2", 1f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", false, 1f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute2", 1f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute2" },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 2, -1, 0f, true },
			new object[] { 1, 2, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute2", false, 1f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 2f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute90", false, 2f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 2f },
		},
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 92, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 0, 0f, true },
			new object[] { 1, 2, 90, 1f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute90", false, 2f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 2f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "TestAttributeSet.Attribute90" },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 90, 1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", "TestAttributeSet.Attribute90", false, 2f, 0f, 0f },
		},
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute90", 2f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.AttributeCurrentValue, "Invalid.Attribute" },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 98, 1f, true },
			new object[] { 1, 1, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		})]
	public void Attributre_based_modifiers_triggers_update_cues_when_attribute_changes_with_expected_results(
		object[] attributeBasedModifiersData,
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] applicationCueTestDatas1,
		object[] updateCueTestDatas1,
		object[] applicationCueTestDatas2,
		object[] updateCueTestDatas2,
		object[] applicationCueTestDatas3,
		object[] updateCueTestDatas3)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData1 = CreateInfiniteEffectData(
			CreateAttributeBasedModifiers(attributeBasedModifiersData),
			true,
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(cueDatas));
		var effect1 = new Effect(effectData1, new EffectOwnership(entity, entity));

		EffectData effectData2 = CreateInfiniteEffectData(
			CreateModifiers(modifiersData),
			true,
			requireModifierSuccessToTriggerCue,
			CreateCueDatas([]));
		var effect2 = new Effect(effectData2, new EffectOwnership(entity, entity));

		ResetCues();

		entity.EffectsManager.ApplyEffect(effect1);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas1);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas1);

		ActiveEffectHandle? activeEffectHandler = entity.EffectsManager.ApplyEffect(effect2);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas2);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas2);

		entity.EffectsManager.UnapplyEffect(activeEffectHandler!);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas3);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas3);
	}

	[Theory]
	[Trait("Update", null)]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		1,
		3,
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		5f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 2, 0.2f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 3, 2, 0.2f, true },
			new object[] { 1, 3, 2, 0.2f, true },
			new object[] { 2, 3, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 4, -2, 0f, true },
			new object[] { 1, 4, 2, 0.2f, true },
			new object[] { 2, 4, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		2,
		3,
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, -1, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 1, 0.1f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 4, 2, 0.2f, true },
			new object[] { 1, 4, 2, 0.2f, true },
			new object[] { 2, 4, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 5, -2, 0f, true },
			new object[] { 1, 5, 2, 0.2f, true },
			new object[] { 2, 5, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		2,
		3,
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		10f,
		40f,
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, -1, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 1, 0.1f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 4, 2, 0.2f, true },
			new object[] { 1, 4, 2, 0.2f, true },
			new object[] { 2, 4, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, false },
			new object[] { 1, 1, 1, 0.1f, false },
			new object[] { 2, 1, 2, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 6, -2, 0f, false },
			new object[] { 1, 6, 2, 0.2f, false },
			new object[] { 2, 6, 1, 0.1f, false },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		1,
		3,
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
			new object[] { 2, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 1, 1, 0.1f, false },
			new object[] { 1, 1, 1, 0.1f, false },
			new object[] { 2, 1, 1, 0.1f, false },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, false },
			new object[] { 1, 0, 0, 0f, false },
			new object[] { 2, 0, 0, 0f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 2, -2, 0f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 1, 0.1f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		3,
		3,
		false,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, -1, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 4, 0, 0f, true },
			new object[] { 1, 4, 2, 0.2f, true },
			new object[] { 2, 4, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 5, -2, 0f, true },
			new object[] { 1, 5, 2, 0.2f, true },
			new object[] { 2, 5, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		3,
		3,
		true,
		false,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
			new object[] { 2, 0, 10, CueMagnitudeType.StackCount },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
			new object[] { 2, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, -1, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 2, 2, 0.2f, true },
			new object[] { 1, 2, 2, 0.2f, true },
			new object[] { 2, 2, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 3, 2, 0.2f, true },
			new object[] { 1, 3, 2, 0.2f, true },
			new object[] { 2, 3, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
			new object[] { 2, 1, 3, 0.3f, true },
		},
		new object[]
		{
			new object[] { 0, 4, -2, 0f, true },
			new object[] { 1, 4, 2, 0.2f, true },
			new object[] { 2, 4, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		3,
		3,
		false,
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 2, 0.2f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		})]
	[InlineData(
		new object[]
		{
			new object[] { "TestAttributeSet.Attribute1", 1f },
		},
		10f,
		3,
		3,
		false,
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeValueChange, "Invalid.Attribute" },
			new object[] { 1, 0, 10, CueMagnitudeType.EffectLevel },
		},
		10f,
		10f,
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 0, 0, 0f, true },
			new object[] { 1, 0, 0, 0f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 1, 0.1f, true },
		},
		new object[]
		{
			new object[] { 0, 1, 0, 0f, true },
			new object[] { 1, 1, 2, 0.2f, true },
		})]
	public void Stackable_effect_triggers_update_cues_when_attribute_changes_with_expected_results(
		object[] attributeBasedModifiersData,
		float duration,
		int initialStack,
		int stackLimit,
		bool requireModifierSuccessToTriggerCue,
		bool suppressStackingCues,
		object[] cueDatas,
		float deltaUpdate1,
		float deltaUpdate2,
		object[] applicationCueTestDatas1,
		object[] updateCueTestDatas1,
		object[] applicationCueTestDatas2,
		object[] updateCueTestDatas2,
		object[] applicationCueTestDatas3,
		object[] updateCueTestDatas3,
		object[] applicationCueTestDatas4,
		object[] updateCueTestDatas4,
		object[] applicationCueTestDatas5,
		object[] updateCueTestDatas5)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData1 = CreateDurationStackableEffectData(
			duration,
			initialStack,
			stackLimit,
			CreateModifiers(attributeBasedModifiersData),
			requireModifierSuccessToTriggerCue,
			suppressStackingCues,
			CreateCueDatas(cueDatas));
		var effect1 = new Effect(effectData1, new EffectOwnership(entity, entity));

		ResetCues();

		entity.EffectsManager.ApplyEffect(effect1);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas1);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas1);

		entity.EffectsManager.UpdateEffects(deltaUpdate1);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas2);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas2);

		effect1.LevelUp();
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas3);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas3);

		entity.EffectsManager.ApplyEffect(effect1);
		entity.EffectsManager.ApplyEffect(effect1);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas4);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas4);

		entity.EffectsManager.UpdateEffects(deltaUpdate2);
		TestCueExecutionData(TestCueExecutionType.Application, applicationCueTestDatas5);
		TestCueExecutionData(TestCueExecutionType.Update, updateCueTestDatas5);
	}

	[Fact]
	[Trait("Invalid cue", null)]
	public void Invalid_cue_fails_gracefullys()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInstantEffectData(
			CreateModifiers([new object[] { "TestAttributeSet.Attribute1", 3f }]),
			false,
			[new CueData(
				Tag.RequestTag(_tagsManager, "invalid.tag", false).GetSingleTagContainer(),
				0,
				10,
				CueMagnitudeType.EffectLevel)]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ResetCues();
		entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Execution, [
				new object[] { 0, 0, 0, 0f, false },
				new object[] { 1, 0, 0, 0f, false }
			]);

		effect.LevelUp();
		entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Execution, [
				new object[] { 0, 0, 0, 0f, false },
				new object[] { 1, 0, 0, 0f, false }
			]);
	}

	[Fact]
	[Trait("Custom cues", null)]
	public void Custom_calculator_class_sets_custom_cues_parameters_correctly()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomMagnitudeCalculator(
			"TestAttributeSet.Attribute1",
			AttributeCaptureSource.Source,
			1);

		var effectData = new EffectData(
			"Level Up",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							customCalculatorClass,
							new ScalableFloat(1),
							new ScalableFloat(0),
							new ScalableFloat(0))))
			],
			cues: [new CueData(
				Tag.RequestTag(_tagsManager, "Test.Cue1").GetSingleTagContainer(),
				0,
				10,
				CueMagnitudeType.EffectLevel)]);

		var effect = new Effect(
			effectData,
			new EffectOwnership(
				new TestEntity(_tagsManager, _cuesManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [2, 2, 0, 0]);

		_testCues[0].ExecuteData.CustomParameters.Should().NotBeNull();
		_testCues[0].ExecuteData.CustomParameters.Should().Contain("test", 1);
	}

	[Fact]
	[Trait("Custom cues", null)]
	public void Custom_executions_sets_custom_cues_parameters_correctly()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		var customCalculatorClass = new CustomTestExecutionClass();

		var effectData = new EffectData(
			"Test Effect",
			new DurationData(DurationType.Instant),
			customExecutions: [customCalculatorClass],
			cues: [new CueData(Tag.RequestTag(_tagsManager, "Test.Cue1").GetSingleTagContainer(), 0, 10, CueMagnitudeType.EffectLevel)]);

		var effect = new Effect(effectData, new EffectOwnership(owner, owner));

		ResetCues();

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [89, 89, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [16, 16, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [10, 10, 0, 0]);

		_testCues[0].ExecuteData.Count.Should().Be(1);
		_testCues[0].ExecuteData.CustomParameters.Should().NotBeNull();
		_testCues[0].ExecuteData.CustomParameters.Should().Contain("custom.parameter", 8);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [88, 88, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [31, 31, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [18, 18, 0, 0]);

		_testCues[0].ExecuteData.Count.Should().Be(2);
		_testCues[0].ExecuteData.CustomParameters.Should().NotBeNull();
		_testCues[0].ExecuteData.CustomParameters.Should().Contain("custom.parameter", 16);

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(owner, "TestAttributeSet.Attribute90", [87, 87, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [46, 46, 0, 0]);
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute2", [26, 26, 0, 0]);

		_testCues[0].ExecuteData.Count.Should().Be(3);
		_testCues[0].ExecuteData.CustomParameters.Should().NotBeNull();
		_testCues[0].ExecuteData.CustomParameters.Should().Contain("custom.parameter", 24);
	}

	[Fact]
	[Trait("Multiple cues", null)]
	public void Effect_triggers_multiple_cues_with_expected_results()
	{
		var tagContainer = new TagContainer(
			_tagsManager,
			[
				Tag.RequestTag(_tagsManager, "test.cue1"),
				Tag.RequestTag(_tagsManager, "test.cue2"),
				Tag.RequestTag(_tagsManager, "test.cue3")
			]);

		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateInstantEffectData(
			CreateModifiers([new object[] { "TestAttributeSet.Attribute1", 3f }]),
			true,
			[new CueData(tagContainer, 0, 10, CueMagnitudeType.AttributeValueChange, "TestAttributeSet.Attribute1")]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ResetCues();
		entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(
			TestCueExecutionType.Execution,
			[
				new object[] { 0, 1, 3, 0.3f, false },
				new object[] { 1, 1, 3, 0.3f, false },
				new object[] { 2, 1, 3, 0.3f, false },
			]);
	}

	private static EffectData CreateInstantEffectData(
		Modifier[] modifiers,
		bool requireModifierSuccessToTriggerCue,
		CueData[] cues)
	{
		return new EffectData(
			"Instant Effect",
			new DurationData(DurationType.Instant),
			modifiers,
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			cues: cues);
	}

	private static EffectData CreateInfiniteEffectData(
		Modifier[] modifiers,
		bool snapshotLevel,
		bool requireModifierSuccessToTriggerCue,
		CueData[] cues)
	{
		return new EffectData(
			"Infinite Effect",
			new DurationData(DurationType.Infinite),
			modifiers,
			snapshopLevel: snapshotLevel,
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			cues: cues);
	}

	private static EffectData CreateDurationPeriodicEffectData(
		float duration,
		float period,
		bool executeOnApplication,
		Modifier[] modifiers,
		bool requireModifierSuccessToTriggerCue,
		CueData[] cues)
	{
		return new EffectData(
			"Infinite Effect",
			new DurationData(DurationType.HasDuration, new ScalableFloat(duration)),
			modifiers,
			periodicData: new PeriodicData(
				new ScalableFloat(period),
				executeOnApplication,
				PeriodInhibitionRemovedPolicy.ResetPeriod),
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			cues: cues);
	}

	private static EffectData CreateDurationStackableEffectData(
		float duration,
		int initialStack,
		int stackLimit,
		Modifier[] modifiers,
		bool requireModifierSuccessToTriggerCue,
		bool suppressStackingCues,
		CueData[] cues)
	{
		return new EffectData(
			"Infinite Effect",
			new DurationData(DurationType.HasDuration, new ScalableFloat(duration)),
			modifiers,
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(initialStack),
				StackPolicy.AggregateBySource,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
				LevelDenialPolicy: LevelComparison.None,
				LevelOverridePolicy: LevelComparison.Lower | LevelComparison.Equal | LevelComparison.Higher,
				LevelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.IncreaseStacks,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication),
			snapshopLevel: false,
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			suppressStackingCues: suppressStackingCues,
			cues: cues);
	}

	private static void TestCueExecutionData(
		CueExecutionData cueExecutionData,
		int count,
		int value,
		float normalizedValue)
	{
		cueExecutionData.Count.Should().Be(count);
		cueExecutionData.Value.Should().Be(value);
		cueExecutionData.NormalizedValue.Should().Be(normalizedValue);
		cueExecutionData.CustomParameters.Should().BeNull();
	}

	private static Modifier[] CreateModifiers(object[] modifiersData)
	{
		var result = new Modifier[modifiersData.Length];

		for (var i = 0; i < modifiersData.Length; i++)
		{
			var modifierData = (object[])modifiersData[i];

			result[i] = new Modifier(
				(string)modifierData[0],
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(
						(float)modifierData[1],
						new Curve([new CurveKey(1, 1), new CurveKey(2, 2)]))),
				1);
		}

		return result;
	}

	private static Modifier[] CreateAttributeBasedModifiers(object[] modifiersData)
	{
		var result = new Modifier[modifiersData.Length];

		for (var i = 0; i < modifiersData.Length; i++)
		{
			var modifierData = (object[])modifiersData[i];

			result[i] = new Modifier(
				(string)modifierData[0],
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(
					MagnitudeCalculationType.AttributeBased,
					attributeBasedFloat: new AttributeBasedFloat(
						new AttributeCaptureDefinition(
							(string)modifierData[1],
							AttributeCaptureSource.Target,
							(bool)modifierData[2]),
						AttributeCalculationType.CurrentValue,
						new ScalableFloat((float)modifierData[3]),
						new ScalableFloat((float)modifierData[4]),
						new ScalableFloat((float)modifierData[5]))));
		}

		return result;
	}

	private CueData[] CreateCueDatas(object[] cueDatas)
	{
		var result = new CueData[cueDatas.Length];

		for (var i = 0; i < cueDatas.Length; i++)
		{
			var cueData = (object[])cueDatas[i];

			result[i] = new CueData(
				Tag.RequestTag(_tagsManager, $"Test.Cue{(int)cueData[0] + 1}").GetSingleTagContainer(),
				(int)cueData[1],
				(int)cueData[2],
				(CueMagnitudeType)cueData[3],
				cueData.Length < 5 ? null : (string)cueData[4]);
		}

		return result;
	}

	private void TestCueExecutionData(TestCueExecutionType cueDataType, object[] cueTestDatas)
	{
		for (var i = 0; i < cueTestDatas.Length; i++)
		{
			var cueTestData = (object[])cueTestDatas[i];
			TestCue testCue = _testCues[(int)cueTestData[0]];

			CueExecutionData cueExecutionData = cueDataType switch
			{
				TestCueExecutionType.Application => testCue.ApplyData,
				TestCueExecutionType.Update => testCue.UpdateData,
				TestCueExecutionType.Execution => testCue.ExecuteData,
				_ => testCue.ExecuteData,
			};

			TestCueExecutionData(
				cueExecutionData,
				(int)cueTestData[1],
				(int)cueTestData[2],
				(float)cueTestData[3]);

			testCue.Applied.Should().Be((bool)cueTestData[4]);
		}
	}

	private void ResetCues()
	{
		foreach (TestCue cue in _testCues)
		{
			cue.Reset();
		}
	}

	private sealed class CustomMagnitudeCalculator : CustomModifierMagnitudeCalculator
	{
		private readonly float _exponent;

		public AttributeCaptureDefinition Attribute1 { get; }

		public CustomMagnitudeCalculator(StringKey attribute, AttributeCaptureSource captureSource, float exponent)
		{
			Attribute1 = new AttributeCaptureDefinition(attribute, captureSource, false);

			AttributesToCapture.Add(Attribute1);

			_exponent = exponent;
		}

		public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
		{
			CustomCueParameters.Add("test", _exponent);
			return (float)Math.Pow(CaptureAttributeMagnitude(Attribute1, effect, target), _exponent);
		}
	}
}
