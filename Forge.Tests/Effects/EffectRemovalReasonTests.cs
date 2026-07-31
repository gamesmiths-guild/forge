// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectRemovalReasonTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Fact]
	[Trait("Removal reason", null)]
	public void Duration_effect_running_out_of_time_reports_expired()
	{
		var handler = new RecordingCueHandler();
		TestEntity target = CreateEntity(handler, out CuesManager _);
		var component = new RecordingEffectComponent();

		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.HasDuration, component));
		RecordingEffectComponent instance = ComponentInstance(handle);

		target.EffectsManager.UpdateEffects(11);

		instance.Unapplied.Should().ContainSingle();
		instance.Unapplied[0].Should().Be((true, EffectRemovalReason.Expired));
		handler.RemoveCount.Should().Be(1);
		handler.LastInterrupted.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal reason", null)]
	public void Duration_effect_taken_away_early_reports_removed()
	{
		var handler = new RecordingCueHandler();
		TestEntity target = CreateEntity(handler, out CuesManager _);
		var component = new RecordingEffectComponent();

		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.HasDuration, component));
		RecordingEffectComponent instance = ComponentInstance(handle);

		target.EffectsManager.RemoveEffect(handle!);

		instance.Unapplied.Should().ContainSingle();
		instance.Unapplied[0].Should().Be((true, EffectRemovalReason.Removed));
		handler.RemoveCount.Should().Be(1);
		handler.LastInterrupted.Should().BeTrue();
	}

	// Regression: infinite effects have no natural end, so removing one is always premature. It used to be reported as
	// a non-interrupted removal because the flag was only forced for HasDuration effects.
	[Theory]
	[Trait("Removal reason", null)]
	[InlineData(false)]
	[InlineData(true)]
	public void Infinite_effect_removal_always_reports_removed(bool forceRemoval)
	{
		var handler = new RecordingCueHandler();
		TestEntity target = CreateEntity(handler, out CuesManager _);
		var component = new RecordingEffectComponent();

		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.Infinite, component));
		RecordingEffectComponent instance = ComponentInstance(handle);

		target.EffectsManager.RemoveEffect(handle!, forceRemoval);

		instance.Unapplied.Should().ContainSingle();
		instance.Unapplied[0].Should().Be((true, EffectRemovalReason.Removed));
		handler.RemoveCount.Should().Be(1);
		handler.LastInterrupted.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal reason", null)]
	public void Stack_removal_reports_the_reason_that_dropped_the_stack()
	{
		var handler = new RecordingCueHandler();
		TestEntity target = CreateEntity(handler, out CuesManager _);
		var component = new RecordingEffectComponent();
		EffectData effectData = CreateStackableEffectData(component);

		ActiveEffectHandle? handle = ApplyEffect(target, effectData);
		ApplyEffect(target, effectData);
		RecordingEffectComponent instance = ComponentInstance(handle);
		handle!.StackCount.Should().Be(2);

		// An explicit removal drops a single stack, and never counts as the effect running its course.
		target.EffectsManager.RemoveEffect(handle);

		handle.StackCount.Should().Be(1);
		instance.Unapplied.Should().ContainSingle();
		instance.Unapplied[0].Should().Be((false, EffectRemovalReason.Removed));

		// Letting the refreshed duration lapse drops the last stack as an expiry.
		target.EffectsManager.UpdateEffects(11);

		instance.Unapplied.Should().HaveCount(2);
		instance.Unapplied[1].Should().Be((true, EffectRemovalReason.Expired));
	}

	// Regression: instant effects built per-application component instances but then raised OnEffectApplied on the
	// shared definitions, so stateful components never saw the callback on the instance holding their state.
	[Fact]
	[Trait("Component instances", null)]
	public void Instant_effect_notifies_the_component_instance_not_the_definition()
	{
		TestEntity target = CreateEntity(new RecordingCueHandler(), out CuesManager _);
		var definition = new RecordingEffectComponent();

		ApplyEffect(target, CreateEffectData(DurationType.Instant, definition));

		definition.AppliedCount.Should().Be(0);
		definition.ExecutedCount.Should().Be(0);

		definition.Instances.Should().ContainSingle();
		definition.Instances[0].AppliedCount.Should().Be(1);
		definition.Instances[0].ExecutedCount.Should().Be(1);
	}

	[Fact]
	[Trait("Component instances", null)]
	public void Each_instant_application_gets_its_own_component_instance()
	{
		TestEntity target = CreateEntity(new RecordingCueHandler(), out CuesManager _);
		var definition = new RecordingEffectComponent();
		EffectData effectData = CreateEffectData(DurationType.Instant, definition);

		ApplyEffect(target, effectData);
		ApplyEffect(target, effectData);

		definition.Instances.Should().HaveCount(2);
		definition.Instances.Should().OnlyContain(x => x.AppliedCount == 1);
	}

	private static RecordingEffectComponent ComponentInstance(ActiveEffectHandle? handle)
	{
		handle.Should().NotBeNull();
		RecordingEffectComponent? instance = handle!.GetComponent<RecordingEffectComponent>();
		instance.Should().NotBeNull();
		return instance!;
	}

	private static ActiveEffectHandle? ApplyEffect(TestEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
	}

	private TestEntity CreateEntity(RecordingCueHandler handler, out CuesManager cuesManager)
	{
		cuesManager = new CuesManager();
		cuesManager.RegisterCue(Tag.RequestTag(_tagsManager, "test.cue1"), handler);
		return new TestEntity(_tagsManager, cuesManager);
	}

	private EffectData CreateEffectData(DurationType durationType, IEffectComponent component)
	{
		ModifierMagnitude? duration = durationType == DurationType.HasDuration
			? new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f))
			: null;

		return new EffectData(
			"Removal Reason Effect",
			new DurationData(durationType, duration),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			effectComponents: [component],
			cues: [CreateCueData()]);
	}

	private EffectData CreateStackableEffectData(IEffectComponent component)
	{
		return new EffectData(
			"Stackable Removal Reason Effect",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.RemoveSingleStackAndRefreshDuration,
				StackOwnerDenialPolicy.AlwaysAllow,
				StackOwnerOverridePolicy.KeepCurrent,
				null,
				null,
				null,
				null,
				StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication),
			effectComponents: [component],
			cues: [CreateCueData()]);
	}

	private CueData CreateCueData()
	{
		return new CueData(
			new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "test.cue1")]),
			0,
			10,
			CueMagnitudeType.EffectLevel);
	}

	/// <summary>
	/// A stateful component that records the lifecycle callbacks it receives. The definition keeps a list of every
	/// instance it spawned, so tests can tell the two apart.
	/// </summary>
	private sealed class RecordingEffectComponent : IEffectComponent
	{
		public List<RecordingEffectComponent> Instances { get; } = [];

		public int AppliedCount { get; private set; }

		public int ExecutedCount { get; private set; }

		public List<(bool Removed, EffectRemovalReason Reason)> Unapplied { get; } = [];

		public IEffectComponent CreateInstance()
		{
			var instance = new RecordingEffectComponent();
			Instances.Add(instance);
			return instance;
		}

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			AppliedCount++;
		}

		public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			ExecutedCount++;
		}

		public void OnActiveEffectUnapplied(
			IForgeEntity target,
			in ActiveEffectEvaluatedData activeEffectEvaluatedData,
			bool removed,
			EffectRemovalReason reason)
		{
			Unapplied.Add((removed, reason));
		}
	}
}
