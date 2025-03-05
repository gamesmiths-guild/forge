// Copyright © 2025 Gamesmiths Guild.
#pragma warning disable SA1118 // ParameterMustNotSpanMultipleLines

using System.Diagnostics;
using FluentAssertions;
using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;
using static Gamesmiths.Forge.Tests.GameplayCues.GameplayCuesManagerFixture;
using static Gamesmiths.Forge.Tests.GameplayCues.GameplayCuesManagerFixture.TestCue;

namespace Gamesmiths.Forge.Tests.GameplayCues;

public class GameplayCueTests(
	GameplayTagsManagerFixture tagsManagerFixture,
	GameplayCuesManagerFixture cuesManagerFixture)
	: IClassFixture<GameplayTagsManagerFixture>, IClassFixture<GameplayCuesManagerFixture>
{
	private enum TestCueExecutionType
	{
		Execution = 0,
		Application = 1,
		Update = 2,
	}

	private readonly GameplayTagsManager _gameplayTagsManager = tagsManagerFixture.GameplayTagsManager;
	private readonly GameplayCuesManager _gameplayCuesManager = cuesManagerFixture.GameplayCuesManager;
	private readonly TestCue[] _testCues = cuesManagerFixture.TestCueInstances;

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
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeDelta, "TestAttributeSet.Attribute1" },
			new object[] { 1, 0, 5, CueMagnitudeType.EffectLevel },
		},
		new object[]
		{
			new object[] { 0, 1, 3, 0.3f, false },
			new object[] { 1, 1, 1, 0.2f, false },
		},
		new object[]
		{
			new object[] { 0, 2, 3, 0.3f, false },
			new object[] { 1, 2, 2, 0.4f, false },
		})]
	public void Instant_effect_triggers_execute_cues_with_expected_results(
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] cueTestDatas1,
		object[] cueTestDatas2)
	{
		var entity = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		GameplayEffectData effectData = CreateInstantEffectData(
			CreateModifiers(modifiersData),
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(entity, cueDatas));
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

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
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeDelta, "TestAttributeSet.Attribute1" },
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
			new object[] { "TestAttributeSet.Attribute1", 0f },
		},
		true,
		new object[]
		{
			new object[] { 0, 0, 10, CueMagnitudeType.AttributeDelta, "TestAttributeSet.Attribute1" },
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
	public void Infinite_effect_triggers_apply_and_remove_cues_with_expected_results(
		object[] modifiersData,
		bool requireModifierSuccessToTriggerCue,
		object[] cueDatas,
		object[] cueTestDatas1,
		object[] cueTestDatas2)
	{
		var entity = new TestEntity(_gameplayTagsManager, _gameplayCuesManager);
		GameplayEffectData effectData = CreateInfiniteEffectData(
			CreateModifiers(modifiersData),
			requireModifierSuccessToTriggerCue,
			CreateCueDatas(entity, cueDatas));
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		ResetCues();
		ActiveGameplayEffectHandle? activeEffectHandler = entity.EffectsManager.ApplyEffect(effect);
		TestCueExecutionData(TestCueExecutionType.Application, cueTestDatas1);

		Debug.Assert(activeEffectHandler is not null, "Effect should not be null here.");
		entity.EffectsManager.UnapplyEffect(activeEffectHandler);
		TestCueExecutionData(TestCueExecutionType.Application, cueTestDatas2);
	}

	private static GameplayEffectData CreateInstantEffectData(
		Modifier[] modifiers,
		bool requireModifierSuccessToTriggerCue,
		GameplayCueData[] cues)
	{
		return new GameplayEffectData(
			"Instant Effect",
			modifiers,
			new DurationData(DurationType.Instant),
			null,
			null,
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			gameplayCues: cues);
	}

	private static GameplayEffectData CreateInfiniteEffectData(
		Modifier[] modifiers,
		bool requireModifierSuccessToTriggerCue,
		GameplayCueData[] cues)
	{
		return new GameplayEffectData(
			"Infinite Effect",
			modifiers,
			new DurationData(DurationType.Infinite),
			null,
			null,
			requireModifierSuccessToTriggerCue: requireModifierSuccessToTriggerCue,
			gameplayCues: cues);
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
	}

	private static GameplayCueData[] CreateCueDatas(TestEntity entity, object[] cueDatas)
	{
		var result = new GameplayCueData[cueDatas.Length];

		for (var i = 0; i < cueDatas.Length; i++)
		{
			var cueData = (object[])cueDatas[i];

			result[i] = new GameplayCueData(
				$"Test.Cue{(int)cueData[0] + 1}",
				(int)cueData[1],
				(int)cueData[2],
				(CueMagnitudeType)cueData[3],
				cueData.Length < 5 ? null : entity.Attributes[(string)cueData[4]]);
		}

		return result;
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
					new ScalableFloat((float)modifierData[1])));
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
}
