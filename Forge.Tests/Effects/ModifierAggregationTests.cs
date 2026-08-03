// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class ModifierAggregationTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute100";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Aggregation", null)]
	[InlineData(10, 25)]
	[InlineData(25, 10)]
	public void Only_the_strongest_modifier_of_a_max_group_contributes(
		float firstMagnitude,
		float secondMagnitude)
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? firstHandle = ApplyEffect(
			target,
			CreateEffectData("First buff", ModifierOperation.FlatBonus, firstMagnitude, AggregationMode.Max));

		ActiveEffectHandle? secondHandle = ApplyEffect(
			target,
			CreateEffectData("Second buff", ModifierOperation.FlatBonus, secondMagnitude, AggregationMode.Max));

		bool firstIsStrongest = firstMagnitude > secondMagnitude;
		ActiveEffectHandle strongestHandle = firstIsStrongest ? firstHandle! : secondHandle!;
		ActiveEffectHandle weakestHandle = firstIsStrongest ? secondHandle! : firstHandle!;

		// Only the strongest buff of the group is active, no matter the order they were applied in.
		TestUtils.TestAttribute(target, TargetAttribute, [125, 100, 25, 0]);

		// And the next strongest one takes over as soon as it's removed.
		target.EffectsManager.RemoveEffect(strongestHandle);

		TestUtils.TestAttribute(target, TargetAttribute, [110, 100, 10, 0]);

		target.EffectsManager.RemoveEffect(weakestHandle);

		TestUtils.TestAttribute(target, TargetAttribute, [100, 100, 0, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Only_the_strongest_modifier_of_a_min_group_contributes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? weakSlowHandle = ApplyEffect(
			target,
			CreateEffectData("Weak slow", ModifierOperation.FlatBonus, -10, AggregationMode.Min));

		TestUtils.TestAttribute(target, TargetAttribute, [90, 100, -10, 0]);

		ActiveEffectHandle? strongSlowHandle = ApplyEffect(
			target,
			CreateEffectData("Strong slow", ModifierOperation.FlatBonus, -25, AggregationMode.Min));

		TestUtils.TestAttribute(target, TargetAttribute, [75, 100, -25, 0]);

		target.EffectsManager.RemoveEffect(strongSlowHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [90, 100, -10, 0]);

		target.EffectsManager.RemoveEffect(weakSlowHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [100, 100, 0, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Aggregation_compares_signed_values_rather_than_magnitudes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Small penalty", ModifierOperation.FlatBonus, -5, AggregationMode.Max));
		ApplyEffect(target, CreateEffectData("Big penalty", ModifierOperation.FlatBonus, -20, AggregationMode.Max));

		// Max picks the highest value, which among penalties is the weakest one. Penalties belong in a min group.
		TestUtils.TestAttribute(target, TargetAttribute, [95, 100, -5, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Each_aggregation_group_contributes_to_the_same_channel()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Summed buff", ModifierOperation.FlatBonus, 5));
		ApplyEffect(target, CreateEffectData("Another summed buff", ModifierOperation.FlatBonus, 3));
		ApplyEffect(target, CreateEffectData("Weak buff", ModifierOperation.FlatBonus, 10, AggregationMode.Max));
		ApplyEffect(target, CreateEffectData("Strong buff", ModifierOperation.FlatBonus, 20, AggregationMode.Max));
		ApplyEffect(target, CreateEffectData("Weak slow", ModifierOperation.FlatBonus, -4, AggregationMode.Min));
		ApplyEffect(target, CreateEffectData("Strong slow", ModifierOperation.FlatBonus, -15, AggregationMode.Min));

		// 100 + (5 + 3) + 20 + (-15)
		TestUtils.TestAttribute(target, TargetAttribute, [113, 100, 13, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Modifiers_with_the_same_magnitude_are_aggregated_as_separate_entries()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? firstHandle = ApplyEffect(
			target,
			CreateEffectData("First buff", ModifierOperation.FlatBonus, 10, AggregationMode.Max));

		ActiveEffectHandle? secondHandle = ApplyEffect(
			target,
			CreateEffectData("Second buff", ModifierOperation.FlatBonus, 10, AggregationMode.Max));

		TestUtils.TestAttribute(target, TargetAttribute, [110, 100, 10, 0]);

		target.EffectsManager.RemoveEffect(firstHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [110, 100, 10, 0]);

		target.EffectsManager.RemoveEffect(secondHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [100, 100, 0, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Groups_are_aggregated_per_channel()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Channel 0 buff", ModifierOperation.FlatBonus, 10, AggregationMode.Max));
		ApplyEffect(
			target,
			CreateEffectData("Channel 1 buff", ModifierOperation.FlatBonus, 20, AggregationMode.Max, channel: 1));

		// Both contribute since they belong to different channels: (100 + 10) + 20.
		TestUtils.TestAttribute(target, TargetAttribute, [130, 100, 30, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Only_the_strongest_percent_modifier_of_a_max_group_contributes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(
			target,
			CreateEffectData("Minor haste", ModifierOperation.PercentBonus, 0.2f, AggregationMode.Max));

		ActiveEffectHandle? majorHasteHandle = ApplyEffect(
			target,
			CreateEffectData("Major haste", ModifierOperation.PercentBonus, 0.5f, AggregationMode.Max));

		TestUtils.TestAttribute(target, TargetAttribute, [150, 100, 50, 0]);

		target.EffectsManager.RemoveEffect(majorHasteHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [120, 100, 20, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void Only_the_strongest_percent_modifier_of_a_min_group_contributes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Weak slow", ModifierOperation.PercentBonus, -0.15f, AggregationMode.Min));
		ApplyEffect(
			target,
			CreateEffectData("Strong slow", ModifierOperation.PercentBonus, -0.3f, AggregationMode.Min));

		TestUtils.TestAttribute(target, TargetAttribute, [70, 100, -30, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void The_next_strongest_modifier_takes_over_when_the_strongest_one_expires()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(
			target,
			CreateEffectData("Long weak buff", ModifierOperation.FlatBonus, 10, AggregationMode.Max, duration: 10));

		ApplyEffect(
			target,
			CreateEffectData("Short strong buff", ModifierOperation.FlatBonus, 25, AggregationMode.Max, duration: 5));

		TestUtils.TestAttribute(target, TargetAttribute, [125, 100, 25, 0]);

		target.EffectsManager.UpdateEffects(6);

		TestUtils.TestAttribute(target, TargetAttribute, [110, 100, 10, 0]);

		target.EffectsManager.UpdateEffects(5);

		TestUtils.TestAttribute(target, TargetAttribute, [100, 100, 0, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void A_re_evaluated_modifier_replaces_its_previous_value_in_the_group()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Static buff", ModifierOperation.FlatBonus, 5, AggregationMode.Max));

		var scalingEffect = new Effect(
			new EffectData(
				"Scaling buff",
				new DurationData(DurationType.Infinite),
				[
					new Modifier(
						TargetAttribute,
						ModifierOperation.FlatBonus,
						new ModifierMagnitude(
							MagnitudeCalculationType.ScalableFloat,
							new ScalableFloat(10, new Curve([new CurveKey(1, 1), new CurveKey(2, 3)]))),
						AggregationMode: AggregationMode.Max)
				],
				snapshotLevel: false),
			CreateOwnership());

		ActiveEffectHandle? scalingHandle = target.EffectsManager.ApplyEffect(scalingEffect);

		TestUtils.TestAttribute(target, TargetAttribute, [110, 100, 10, 0]);

		scalingEffect.LevelUp();

		TestUtils.TestAttribute(target, TargetAttribute, [130, 100, 30, 0]);

		// If the re-evaluation had left the old magnitude behind, the group would still hold a stale 10.
		target.EffectsManager.RemoveEffect(scalingHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [105, 100, 5, 0]);
	}

	[Theory]
	[Trait("Aggregation", null)]
	[InlineData(50, 30)]
	[InlineData(30, 50)]
	public void The_strongest_override_of_a_max_group_wins(float firstMagnitude, float secondMagnitude)
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle? firstHandle = ApplyEffect(
			target,
			CreateEffectData("First override", ModifierOperation.Override, firstMagnitude, AggregationMode.Max));

		ActiveEffectHandle? secondHandle = ApplyEffect(
			target,
			CreateEffectData("Second override", ModifierOperation.Override, secondMagnitude, AggregationMode.Max));

		TestUtils.TestAttribute(target, TargetAttribute, [50, 100, -50, 0]);

		target.EffectsManager.RemoveEffect(firstMagnitude > secondMagnitude ? firstHandle! : secondHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [30, 100, -70, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void The_weakest_override_of_a_min_group_wins()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Snare", ModifierOperation.Override, 10, AggregationMode.Min));
		ActiveEffectHandle? rootHandle = ApplyEffect(
			target,
			CreateEffectData("Root", ModifierOperation.Override, 0, AggregationMode.Min));

		TestUtils.TestAttribute(target, TargetAttribute, [0, 100, -100, 0]);

		target.EffectsManager.RemoveEffect(rootHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [10, 100, -90, 0]);
	}

	[Fact]
	[Trait("Aggregation", null)]
	public void The_most_recent_override_decides_which_group_arbitrates_the_channel()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		ApplyEffect(target, CreateEffectData("Strong override", ModifierOperation.Override, 50, AggregationMode.Max));
		ApplyEffect(target, CreateEffectData("Weak override", ModifierOperation.Override, 30, AggregationMode.Max));

		TestUtils.TestAttribute(target, TargetAttribute, [50, 100, -50, 0]);

		// A plain override is last-applied-wins, so it takes the channel from the max group while it's active.
		ActiveEffectHandle? plainHandle = ApplyEffect(
			target,
			CreateEffectData("Plain override", ModifierOperation.Override, 20));

		TestUtils.TestAttribute(target, TargetAttribute, [20, 100, -80, 0]);

		target.EffectsManager.RemoveEffect(plainHandle!);

		TestUtils.TestAttribute(target, TargetAttribute, [50, 100, -50, 0]);
	}

	private static EffectData CreateEffectData(
		string effectName,
		ModifierOperation operation,
		float magnitude,
		AggregationMode aggregationMode = AggregationMode.Sum,
		int channel = 0,
		float? duration = null)
	{
		DurationData durationData = duration.HasValue
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration.Value)))
			: new DurationData(DurationType.Infinite);

		return new EffectData(
			effectName,
			durationData,
			[
				new Modifier(
					TargetAttribute,
					operation,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)),
					channel,
					aggregationMode)
			]);
	}

	private ActiveEffectHandle? ApplyEffect(TestEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, CreateOwnership()));
	}

	private EffectOwnership CreateOwnership()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		return new EffectOwnership(owner, owner);
	}
}
