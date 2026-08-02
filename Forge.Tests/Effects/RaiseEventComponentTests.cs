// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class RaiseEventComponentTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Trigger", null)]
	public void Application_raises_on_the_target()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		target.EffectsManager.ApplyEffect(CreateEffect(target, target, eventTag, EffectEventTrigger.Applied));

		received.Should().ContainSingle();
	}

	[Fact]
	[Trait("Trigger", null)]
	public void An_instant_effect_raises_on_application()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		target.EffectsManager.ApplyEffect(
			CreateEffect(target, target, eventTag, EffectEventTrigger.Applied, DurationType.Instant));

		received.Should().ContainSingle();
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Execution_raises_once_per_tick()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		target.EffectsManager.ApplyEffect(
			CreateEffect(target, target, eventTag, EffectEventTrigger.Executed, periodic: true));

		target.EffectsManager.UpdateEffects(1);
		target.EffectsManager.UpdateEffects(1);

		// Once on application and once per period.
		received.Should().HaveCount(3);
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Removal_raises_when_the_effect_is_fully_removed()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			CreateEffect(target, target, eventTag, EffectEventTrigger.Removed))!;

		received.Should().BeEmpty();

		target.EffectsManager.RemoveEffect(handle, true);

		received.Should().ContainSingle();
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Stack_removal_raises_for_each_stack_the_effect_survives()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		EffectData effectData = CreateEffectData(
			eventTag,
			EffectEventTrigger.StackRemoved,
			stackable: true);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)))!;
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		handle.StackCount.Should().Be(3);
		received.Should().BeEmpty();

		target.EffectsManager.RemoveEffect(handle, 1);
		received.Should().ContainSingle();

		target.EffectsManager.RemoveEffect(handle, 1);
		received.Should().HaveCount(2);

		// The stack that takes the count to zero is a full removal, so it reports Removed rather than StackRemoved.
		target.EffectsManager.RemoveEffect(handle, 1);
		handle.IsValid.Should().BeFalse();
		received.Should().HaveCount(2);
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Stack_removal_and_removal_are_separate_triggers()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		EffectData effectData = CreateEffectData(
			eventTag,
			EffectEventTrigger.Removed | EffectEventTrigger.StackRemoved,
			stackable: true);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)))!;
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		// One stack lost and then the effect gone: two raises, one per trigger.
		target.EffectsManager.RemoveEffect(handle, 1);
		target.EffectsManager.RemoveEffect(handle, true);

		received.Should().HaveCount(2);
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Stack_removal_reports_the_count_before_the_stack_was_taken()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		// A custom calculator is how a designer reaches the stack count, since the evaluated data reaches it and a
		// plain ScalableFloat does not.
		EffectData effectData = CreateEffectData(
			eventTag,
			EffectEventTrigger.StackRemoved,
			magnitude: new ModifierMagnitude(
				MagnitudeCalculationType.CustomCalculatorClass,
				customCalculationBasedFloat: new CustomCalculationBasedFloat(
					new StackCountCalculator(),
					new ScalableFloat(1),
					new ScalableFloat(0),
					new ScalableFloat(0))),
			stackable: true);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)))!;
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		target.EffectsManager.RemoveEffect(handle, 1);

		// Three stacks at the moment the hook fired, dropping to two right after.
		received.Should().ContainSingle().Which.EventMagnitude.Should().Be(3);
		handle.StackCount.Should().Be(2);
	}

	[Fact]
	[Trait("Trigger", null)]
	public void Triggers_combine()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(CreateEffect(
			target,
			target,
			eventTag,
			EffectEventTrigger.Applied | EffectEventTrigger.Removed))!;

		target.EffectsManager.RemoveEffect(handle, true);

		received.Should().HaveCount(2);
	}

	[Fact]
	[Trait("Target", null)]
	public void The_event_reaches_every_named_entity()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();

		List<EventData> onTarget = Listen(target, eventTag);
		List<EventData> onSource = Listen(source, eventTag);

		target.EffectsManager.ApplyEffect(CreateEffect(
			source,
			target,
			eventTag,
			EffectEventTrigger.Applied,
			raiseOn: [EffectApplicationTarget.Target, EffectApplicationTarget.Source]));

		onTarget.Should().ContainSingle();
		onSource.Should().ContainSingle();
	}

	[Fact]
	[Trait("Target", null)]
	public void Source_and_target_describe_the_effect_on_whichever_bus_carries_it()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();

		List<EventData> onSource = Listen(source, eventTag);

		target.EffectsManager.ApplyEffect(CreateEffect(
			source,
			target,
			eventTag,
			EffectEventTrigger.Applied,
			raiseOn: [EffectApplicationTarget.Source]));

		// Raised on the source's bus, but it still reads as "source did this to target" rather than flipping around.
		EventData raised = onSource.Should().ContainSingle().Subject;
		raised.Source.Should().BeSameAs(source);
		raised.Target.Should().BeSameAs(target);
		raised.Payload.Should().BeOfType<Effect>();
	}

	[Fact]
	[Trait("Target", null)]
	public void An_entity_the_effect_does_not_have_is_skipped()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		// No source on the ownership, so the Source entry has nowhere to raise; the Target entry still does.
		EffectData effectData = CreateEffectData(
			eventTag,
			EffectEventTrigger.Applied,
			raiseOn: [EffectApplicationTarget.Source, EffectApplicationTarget.Target]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, null)));

		received.Should().ContainSingle();
	}

	[Fact]
	[Trait("Magnitude", null)]
	public void The_magnitude_scales_with_the_effects_level()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		EffectData effectData = CreateEffectData(
			eventTag,
			EffectEventTrigger.Applied,
			magnitude: new ModifierMagnitude(
				MagnitudeCalculationType.ScalableFloat,
				new ScalableFloat(10, new Curve([new CurveKey(1, 1), new CurveKey(2, 3)]))));

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target), 2));

		received.Should().ContainSingle().Which.EventMagnitude.Should().Be(30);
	}

	[Fact]
	[Trait("Magnitude", null)]
	public void The_magnitude_can_read_a_running_tally_from_the_same_effect()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		var tallyTag = Tag.RequestTag(_tagsManager, "color.red");
		List<EventData> received = Listen(target, eventTag);

		// The accumulator publishes what the tick took off, and the event reports it. Two things have to line up: the
		// components are ordered so the tally lands before the event reads it, both hanging off OnEffectExecuted, and
		// the SetByCallerFloat is declared non-snapshot. A snapshot one caches the first value it ever reads and would
		// report 6 forever.
		var effectData = new EffectData(
			"Reported Drain",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-6)))
			],
			periodicData: new PeriodicData(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(TargetAttribute, tallyTag),
				new RaiseEventEffectComponent(
					eventTag.GetSingleTagContainer()!,
					EffectEventTrigger.Executed,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(tallyTag, false)))
			]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		target.EffectsManager.UpdateEffects(1);

		received.Select(x => x.EventMagnitude).Should().Equal(6, 12);
	}

	[Fact]
	[Trait("Magnitude", null)]
	public void An_omitted_magnitude_reports_zero()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag eventTag = EventTag();
		List<EventData> received = Listen(target, eventTag);

		target.EffectsManager.ApplyEffect(CreateEffect(target, target, eventTag, EffectEventTrigger.Applied));

		received.Should().ContainSingle().Which.EventMagnitude.Should().Be(0);
	}

	[Fact]
	[Trait("Trigger", null)]
	public void An_unmatched_subscription_hears_nothing()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		List<EventData> received = Listen(target, Tag.RequestTag(_tagsManager, "color.blue"));

		target.EffectsManager.ApplyEffect(CreateEffect(target, target, EventTag(), EffectEventTrigger.Applied));

		received.Should().BeEmpty();
	}

	private static Effect CreateEffect(
		TestEntity source,
		TestEntity target,
		Tag eventTag,
		EffectEventTrigger triggers,
		DurationType durationType = DurationType.HasDuration,
		bool periodic = false,
		EffectApplicationTarget[]? raiseOn = null)
	{
		return new Effect(
			CreateEffectData(eventTag, triggers, durationType, periodic, raiseOn),
			new EffectOwnership(target, source));
	}

	private static EffectData CreateEffectData(
		Tag eventTag,
		EffectEventTrigger triggers,
		DurationType durationType = DurationType.HasDuration,
		bool periodic = false,
		EffectApplicationTarget[]? raiseOn = null,
		ModifierMagnitude? magnitude = null,
		bool stackable = false)
	{
		ModifierMagnitude? duration = durationType == DurationType.HasDuration
			? new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))
			: null;

		PeriodicData? periodicData = periodic
			? new PeriodicData(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset)
			: null;

		var component = new RaiseEventEffectComponent(
			eventTag.GetSingleTagContainer()!,
			triggers,
			magnitude,
			raiseOn);

		return new EffectData(
			"Reporting Effect",
			new DurationData(durationType, duration),
			stackingData: stackable ? CreateStackingData() : null,
			periodicData: periodicData,
			effectComponents: [component]);
	}

	private static StackingData CreateStackingData()
	{
		return new StackingData(
			new ScalableInt(5),
			new ScalableInt(1),
			StackPolicy.AggregateBySource,
			StackLevelPolicy.SegregateLevels,
			StackMagnitudePolicy.DontStack,
			StackOverflowPolicy.DenyApplication,
			StackExpirationPolicy.ClearEntireStack,
			ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication);
	}

	private static List<EventData> Listen(TestEntity entity, Tag eventTag)
	{
		List<EventData> received = [];
		entity.Events.Subscribe(eventTag, received.Add);

		return received;
	}

	private Tag EventTag()
	{
		return Tag.RequestTag(_tagsManager, "tag");
	}

	/// <summary>
	/// Reports the effect's stack count, which the evaluated data carries and a plain
	/// <see cref="ScalableFloat"/> has no way to reach.
	/// </summary>
	private sealed class StackCountCalculator : CustomModifierMagnitudeCalculator
	{
		public override float CalculateBaseMagnitude(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			return effectEvaluatedData?.Stack ?? 0;
		}
	}
}
