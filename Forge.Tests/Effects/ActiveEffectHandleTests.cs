// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class ActiveEffectHandleTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Handle", null)]
	public void Duration_effect_handle_exposes_remaining_and_total_duration()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateDurationEffectData(10f));

		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();
		handle.RemainingDuration.Should().BeApproximately(10, 0.001);
		handle.TotalDuration.Should().BeApproximately(10, 0.001);

		target.EffectsManager.UpdateEffects(4);

		handle.RemainingDuration.Should().BeApproximately(6, 0.001);
		handle.TotalDuration.Should().BeApproximately(10, 0.001);
	}

	[Fact]
	[Trait("Handle", null)]
	public void Infinite_effect_handle_returns_minus_one_durations()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateInfiniteEffectData());

		handle.Should().NotBeNull();
		handle!.RemainingDuration.Should().Be(-1);
		handle.TotalDuration.Should().Be(-1);
	}

	[Fact]
	[Trait("Handle", null)]
	public void Invalid_handle_returns_default_values()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateDurationEffectData(10f));

		handle.Should().NotBeNull();
		target.EffectsManager.RemoveEffect(handle!, forceRemoval: true);

		handle!.IsValid.Should().BeFalse();
		handle.RemainingDuration.Should().Be(0);
		handle.TotalDuration.Should().Be(0);
		handle.StackCount.Should().Be(0);
		handle.Level.Should().Be(0);
		handle.ExecutionCount.Should().Be(0);
		handle.Period.Should().Be(0);
		handle.Target.Should().BeNull();
		handle.Effect.Should().BeNull();
	}

	[Fact]
	[Trait("Handle", null)]
	public void Stackable_effect_handle_exposes_stack_count()
	{
		TestEntity owner = CreateEntity();
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData(stackLimit: 3, initialStack: 1);

		ActiveEffectHandle? firstHandle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(owner, owner)));
		ActiveEffectHandle? secondHandle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(owner, owner)));

		firstHandle.Should().NotBeNull();
		secondHandle.Should().NotBeNull();
		firstHandle!.StackCount.Should().Be(2);
		secondHandle!.StackCount.Should().Be(2);
	}

	[Fact]
	[Trait("Handle", null)]
	public void Handle_exposes_effect_level_target_and_instance()
	{
		TestEntity owner = CreateEntity();
		TestEntity target = CreateEntity();
		var effect = new Effect(CreateDurationEffectData(10f), new EffectOwnership(owner, owner), 3);

		ActiveEffectHandle? handle = target.EffectsManager.ApplyEffect(effect);

		handle.Should().NotBeNull();
		handle!.Level.Should().Be(3);
		handle.Target.Should().Be(target);
		handle.Effect.Should().BeSameAs(effect);
	}

	[Fact]
	[Trait("Handle", null)]
	public void Periodic_effect_handle_exposes_period_and_execution_count()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreatePeriodicEffectData(duration: 10f, period: 1f));

		handle.Should().NotBeNull();
		handle!.Period.Should().BeApproximately(1, 0.001);
		handle.ExecutionCount.Should().Be(1);

		target.EffectsManager.UpdateEffects(1);

		handle.ExecutionCount.Should().Be(2);
	}

	[Fact]
	[Trait("Handle", null)]
	public void RefreshDuration_resets_remaining_duration()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateDurationEffectData(10f));

		target.EffectsManager.UpdateEffects(4);
		handle!.RemainingDuration.Should().BeApproximately(6, 0.001);

		handle.RefreshDuration();

		handle.RemainingDuration.Should().BeApproximately(10, 0.001);
	}

	[Fact]
	[Trait("Handle", null)]
	public void RefreshDuration_does_nothing_for_infinite_effects()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateInfiniteEffectData());

		handle!.RefreshDuration();

		handle.RemainingDuration.Should().Be(-1);
	}

	[Fact]
	[Trait("Handle", null)]
	public void GetActiveEffects_returns_handles_for_matching_data()
	{
		TestEntity target = CreateEntity();
		EffectData buffData = CreateDurationEffectData(10f);
		EffectData otherData = CreateInfiniteEffectData();

		ActiveEffectHandle? firstHandle = ApplyEffect(target, buffData);
		ActiveEffectHandle? secondHandle = ApplyEffect(target, buffData);
		ApplyEffect(target, otherData);

		target.EffectsManager.GetActiveEffects(buffData).Should()
			.BeEquivalentTo([firstHandle, secondHandle]);
		target.EffectsManager.GetActiveEffects().Should().HaveCount(3);

		target.EffectsManager.RemoveEffect(firstHandle!, forceRemoval: true);

		target.EffectsManager.GetActiveEffects(buffData).Should()
			.BeEquivalentTo([secondHandle]);
		target.EffectsManager.GetActiveEffects().Should().HaveCount(2);
	}

	private static EffectData CreateDurationEffectData(float duration)
	{
		return new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			]);
	}

	private static EffectData CreateInfiniteEffectData()
	{
		return new EffectData(
			"Infinite Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			]);
	}

	private static EffectData CreatePeriodicEffectData(float duration, float period)
	{
		return new EffectData(
			"Periodic Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private static EffectData CreateStackableEffectData(int stackLimit, int initialStack)
	{
		return new EffectData(
			"Stackable Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(initialStack),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				StackOwnerDenialPolicy.AlwaysAllow,
				StackOwnerOverridePolicy.Override,
				StackOwnerOverrideStackCountPolicy.IncreaseStacks));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private ActiveEffectHandle? ApplyEffect(TestEntity target, EffectData effectData)
	{
		TestEntity owner = CreateEntity();
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner)));
	}
}
