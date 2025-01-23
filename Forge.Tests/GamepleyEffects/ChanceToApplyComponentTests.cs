// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class ChanceToApplyComponentTests(GameplayTagsManagerFixture fixture)
	: IClassFixture<GameplayTagsManagerFixture>
{
	private readonly GameplayTagsManager _gameplayTagsManager = fixture.GameplayTagsManager;

	[Theory]
	[Trait("Instant", null)]
	[InlineData("TestAttributeSet.Attribute1", 10, 21, 0.5f, new[] { 0.4f, 0.6f, 0.2f })]
	[InlineData("TestAttributeSet.Attribute1", 10, 1, 0.1f, new[] { 0.4f, 0.6f, 0.2f })]
	[InlineData("TestAttributeSet.Attribute1", 10, 31, 0.9f, new[] { 0.4f, 0.6f, 0.2f })]
	public void Applies_effect_based_on_drawed_chance(
		string targetAttribute,
		int effectMagnitude,
		int expectedResult,
		float chance,
		float[] randomValues)
	{
		var owner = new TestEntity(_gameplayTagsManager);
		var target = new TestEntity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(effectMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null,
			gameplayEffectComponents: [new ChanceToApplyEffectComponent(
				new FixedRandomProvider(randomValues),
				new ScalableFloat(chance))]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				new TestEntity(_gameplayTagsManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);
		target.EffectsManager.ApplyEffect(effect);
		target.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(target, targetAttribute, [expectedResult, expectedResult, 0, 0]);
	}

	[Theory]
	[Trait("Instant", null)]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		new[] { 1, 1, 11 },
		0.2f,
		new[] { 1f, 2f, 3f },
		new[] { 0.5f, 0.5f, 0.5f })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		new[] { 11, 11, 11 },
		0.2f,
		new[] { 3f, 2f, 1f },
		new[] { 0.5f, 0.5f, 0.5f })]
	[InlineData(
		"TestAttributeSet.Attribute1",
		10,
		new[] { 11, 21, 21 },
		0.2f,
		new[] { 3f, 2f, 1f },
		new[] { 0.2f, 0.2f, 0.2f })]
	public void Chance_to_apply_changes_with_effect_level(
		string targetAttribute,
		int effectMagnitude,
		int[] expectedResults,
		float baseChance,
		float[] levelChanceMultipliers,
		float[] randomValues)
	{
		var owner = new TestEntity(_gameplayTagsManager);
		var target = new TestEntity(_gameplayTagsManager);

		var effectData = new GameplayEffectData(
			"Level Up",
			[
				new Modifier(
					targetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(effectMagnitude)))
			],
			new DurationData(DurationType.Instant),
			null,
			null,
			gameplayEffectComponents: [new ChanceToApplyEffectComponent(
				new FixedRandomProvider(randomValues),
				new ScalableFloat(
					baseChance,
					new Curve(
						[
							new CurveKey(1, levelChanceMultipliers[0]),
							new CurveKey(2, levelChanceMultipliers[1]),
							new CurveKey(3, levelChanceMultipliers[2])
						])))]);

		var effect = new GameplayEffect(
			effectData,
			new GameplayEffectOwnership(
				new TestEntity(_gameplayTagsManager),
				owner));

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, targetAttribute, [expectedResults[0], expectedResults[0], 0, 0]);

		effect.LevelUp();

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, targetAttribute, [expectedResults[1], expectedResults[1], 0, 0]);

		effect.LevelUp();

		target.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(target, targetAttribute, [expectedResults[2], expectedResults[2], 0, 0]);
	}

	private class FixedRandomProvider(params float[] values) : IRandom
	{
		private readonly Queue<float> _values = new(values);

		public int NextInt()
		{
			throw new NotImplementedException();
		}

		public int NextInt(int maxValue)
		{
			throw new NotImplementedException();
		}

		public int NextInt(int minValue, int maxValue)
		{
			throw new NotImplementedException();
		}

		public float NextSingle()
		{
			return _values.Dequeue();
		}

		public double NextDouble()
		{
			throw new NotImplementedException();
		}

		public long NextInt64()
		{
			throw new NotImplementedException();
		}

		public long NextInt64(long maxValue)
		{
			throw new NotImplementedException();
		}

		public long NextInt64(long minValue, long maxValue)
		{
			throw new NotImplementedException();
		}

		public void NextBytes(byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public void NextBytes(Span<byte> buffer)
		{
			throw new NotImplementedException();
		}
	}
}
