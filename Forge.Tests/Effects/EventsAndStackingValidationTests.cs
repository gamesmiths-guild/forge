// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Tests.Effects;

public sealed class EventsAndStackingValidationTests : IDisposable
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = new(["tag", "other.tag"]);

	public EventsAndStackingValidationTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void Raising_on_execution_from_a_non_periodic_duration_effect_is_rejected()
	{
		Action act = () => _ = CreateData(
			DurationType.HasDuration,
			RaiseEvent(EffectEventTrigger.Executed));

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("RaiseEvent", null)]
	[InlineData(DurationType.Instant, false)]
	[InlineData(DurationType.HasDuration, true)]
	[InlineData(DurationType.Infinite, true)]
	public void Raising_on_execution_from_an_effect_that_executes_is_accepted(DurationType durationType, bool periodic)
	{
		Action act = () => _ = CreateData(
			durationType,
			RaiseEvent(EffectEventTrigger.Executed),
			periodic: periodic);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void Raising_on_removal_from_an_instant_effect_is_rejected()
	{
		Action act = () => _ = CreateData(DurationType.Instant, RaiseEvent(EffectEventTrigger.Removed));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void Raising_on_stack_removal_from_a_non_stackable_effect_is_rejected()
	{
		Action act = () => _ = CreateData(DurationType.HasDuration, RaiseEvent(EffectEventTrigger.StackRemoved));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void Raising_on_stack_removal_from_a_stackable_effect_is_accepted()
	{
		Action act = () => _ = CreateData(
			DurationType.HasDuration,
			RaiseEvent(EffectEventTrigger.StackRemoved),
			stackable: true);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void A_component_with_no_trigger_is_rejected()
	{
		Action act = () => _ = CreateData(DurationType.HasDuration, RaiseEvent(EffectEventTrigger.None));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("RaiseEvent", null)]
	public void A_component_with_no_event_tags_is_rejected()
	{
		Action act = () => _ = CreateData(
			DurationType.HasDuration,
			new RaiseEventEffectComponent(new TagContainer(_tagsManager)));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("StackThreshold", null)]
	public void A_threshold_on_a_non_stackable_effect_is_rejected()
	{
		Action act = () => _ = CreateData(DurationType.HasDuration, StackThreshold(2));

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("StackThreshold", null)]
	[InlineData(0)]
	[InlineData(1)]
	public void A_threshold_of_one_or_less_is_rejected(int threshold)
	{
		Action act = () => _ = CreateData(DurationType.HasDuration, StackThreshold(threshold), stackable: true);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("StackThreshold", null)]
	public void A_sustained_threshold_with_an_instant_effect_is_rejected()
	{
		Action act = () => _ = CreateData(
			DurationType.HasDuration,
			StackThreshold(2, thresholdDuration: DurationType.Instant),
			stackable: true);

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("StackThreshold", null)]
	public void An_instant_threshold_effect_is_accepted_when_it_is_never_taken_back()
	{
		// Nothing has to be taken back, so there is nothing for an instant effect to be missing.
		Action act = () => _ = CreateData(
			DurationType.HasDuration,
			StackThreshold(2, ConditionalEffectRemovalPolicy.Ignore, DurationType.Instant),
			stackable: true);

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("Accumulator", null)]
	public void An_accumulator_on_a_non_periodic_duration_effect_is_rejected()
	{
		Action act = () => _ = CreateData(DurationType.HasDuration, Accumulator());

		act.Should().Throw<ValidationException>();
	}

	[Theory]
	[Trait("Accumulator", null)]
	[InlineData(DurationType.Instant, false)]
	[InlineData(DurationType.HasDuration, true)]
	[InlineData(DurationType.Infinite, true)]
	public void An_accumulator_on_an_effect_that_executes_is_accepted(DurationType durationType, bool periodic)
	{
		Action act = () => _ = CreateData(durationType, Accumulator(), periodic: periodic);

		act.Should().NotThrow();
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

	private static EffectData CreateData(
		DurationType durationType,
		IEffectComponent component,
		bool periodic = false,
		bool stackable = false)
	{
		ModifierMagnitude? duration = durationType == DurationType.HasDuration
			? new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))
			: null;

		PeriodicData? periodicData = periodic
			? new PeriodicData(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset)
			: null;

		return new EffectData(
			"Validated Effect",
			new DurationData(durationType, duration),
			stackingData: stackable ? CreateStackingData() : null,
			periodicData: periodicData,
			effectComponents: [component]);
	}

	private StackThresholdEffectComponent StackThreshold(
		int threshold,
		ConditionalEffectRemovalPolicy policy = ConditionalEffectRemovalPolicy.RemoveOnEnd,
		DurationType thresholdDuration = DurationType.Infinite)
	{
		return new StackThresholdEffectComponent(
			threshold,
			[
				new ConditionalEffect(
					CreateData(thresholdDuration, RaiseEvent(EffectEventTrigger.Applied)),
					RemovalPolicy: policy)
			]);
	}

	private RaiseEventEffectComponent RaiseEvent(EffectEventTrigger triggers)
	{
		return new RaiseEventEffectComponent(
			new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "tag")]),
			triggers);
	}

	private AttributeAccumulatorEffectComponent Accumulator()
	{
		return new AttributeAccumulatorEffectComponent(
			TargetAttribute,
			Tag.RequestTag(_tagsManager, "tag"));
	}
}
