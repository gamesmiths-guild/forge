// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class AttributeAccumulatorComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private const string FullAttribute = "TestAttributeSet.Attribute90";
	private const string NearlyEmptyAttribute = "TestAttributeSet.Attribute5";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	private static PeriodicData Periodic =>
		new(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset);

	[Fact]
	[Trait("Policy", null)]
	public void Losses_tallies_what_the_effect_took_off_as_a_positive_total()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		Effect effect = CreateDrain(target, tallyTag, AccumulationPolicy.Losses, FullAttribute, -5);
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.UpdateEffects(1);
		target.EffectsManager.UpdateEffects(1);

		// Once on application and once per tick.
		TallyOf(handle).Should().Be(15);
		PublishedTally(effect, tallyTag).Should().Be(15);
	}

	[Fact]
	[Trait("Policy", null)]
	public void Losses_stops_counting_once_the_attribute_bottoms_out()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		// The attribute holds 5 and each tick aims 5 at it, so the second tick lands on an empty pool.
		Effect effect = CreateDrain(target, tallyTag, AccumulationPolicy.Losses, NearlyEmptyAttribute, -5);
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.UpdateEffects(1);

		target.Attributes[NearlyEmptyAttribute].CurrentValue.Should().Be(0);
		TallyOf(handle).Should().Be(5);
	}

	[Fact]
	[Trait("Policy", null)]
	public void Gains_tallies_what_was_restored_and_not_what_was_aimed()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		// The attribute holds 90 out of a maximum of 99, so the second tick can only land 4 of its 5.
		Effect effect = CreateDrain(target, tallyTag, AccumulationPolicy.Gains, FullAttribute, 5);
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.UpdateEffects(1);

		target.Attributes[FullAttribute].CurrentValue.Should().Be(99);
		TallyOf(handle).Should().Be(9);
	}

	[Fact]
	[Trait("Policy", null)]
	public void Net_reports_a_signed_total()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		Effect effect = CreateDrain(target, tallyTag, AccumulationPolicy.Net, FullAttribute, -5);
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.UpdateEffects(1);

		// Net keeps the direction of the movement, unlike Losses, which reports the same drain as a positive 10.
		TallyOf(handle).Should().Be(-10);
	}

	[Fact]
	[Trait("Policy", null)]
	public void Every_policy_measures_the_attribute_rather_than_the_modifiers()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var lossesTag = Tag.RequestTag(_tagsManager, "color.red");
		var gainsTag = Tag.RequestTag(_tagsManager, "color.green");
		var netTag = Tag.RequestTag(_tagsManager, "color.blue");

		// Two modifiers pulling in opposite directions on one attribute: +7 then -5, so each execution moves it by +2.
		// A tally summing ModifiersEvaluatedData would see a 7 and a 5; all three policies see only the +2.
		var effectData = new EffectData(
			"Push And Pull",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[
				Flat(FullAttribute, 7),
				Flat(FullAttribute, -5)
			],
			periodicData: Periodic,
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(FullAttribute, lossesTag, AccumulationPolicy.Losses),
				new AttributeAccumulatorEffectComponent(FullAttribute, gainsTag, AccumulationPolicy.Gains),
				new AttributeAccumulatorEffectComponent(FullAttribute, netTag, AccumulationPolicy.Net)
			]);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)))!;

		target.Attributes[FullAttribute].CurrentValue.Should().Be(92);

		// Three accumulators on one effect, each publishing under its own tag. This is how two attributes are tallied
		// side by side as well: one component each, read separately or added together by whatever consumes them.
		TalliesOf(handle).Should().BeEquivalentTo([0f, 2f, 2f]);
	}

	[Fact]
	[Trait("Baseline", null)]
	public void Changes_from_other_sources_are_not_counted()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		Effect effect = CreateDrain(target, tallyTag, AccumulationPolicy.Losses, FullAttribute, -5);
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		// Something else takes a large bite out of the same attribute between two of the drain's ticks.
		target.EffectsManager.ApplyEffect(new Effect(
			new EffectData("Unrelated Hit", new DurationData(DurationType.Instant), [Flat(FullAttribute, -40)]),
			new EffectOwnership(target, target)));

		target.EffectsManager.UpdateEffects(1);

		target.Attributes[FullAttribute].CurrentValue.Should().Be(40);

		// The drain only ever answers for its own two executions; the subscription absorbed the 40 in between.
		TallyOf(handle).Should().Be(10);
	}

	[Fact]
	[Trait("Baseline", null)]
	public void Each_application_keeps_its_own_total()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		ActiveEffectHandle first = target.EffectsManager.ApplyEffect(
			CreateDrain(target, tallyTag, AccumulationPolicy.Losses, FullAttribute, -5))!;

		target.EffectsManager.UpdateEffects(1);

		ActiveEffectHandle second = target.EffectsManager.ApplyEffect(
			CreateDrain(target, tallyTag, AccumulationPolicy.Losses, FullAttribute, -5))!;

		target.EffectsManager.UpdateEffects(1);

		TallyOf(first).Should().Be(15);
		TallyOf(second).Should().Be(10);
	}

	[Fact]
	[Trait("Seeding", null)]
	public void An_effect_removed_before_it_executes_resolves_to_zero()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		// Ticking only on the period, so nothing has executed by the time it is removed.
		var effectData = new EffectData(
			"Slow Drain",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[Flat(FullAttribute, -5)],
			periodicData: new PeriodicData(new ScalableFloat(1), false, PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents: [new AttributeAccumulatorEffectComponent(FullAttribute, tallyTag)]);

		var effect = new Effect(effectData, new EffectOwnership(target, target));
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.RemoveEffect(handle, true);

		// A SetByCaller magnitude reads the dictionary with a plain indexer, so an unseeded tag would throw here
		// rather than resolve to nothing.
		effect.DataTag.Should().ContainKey(tallyTag);
		PublishedTally(effect, tallyTag).Should().Be(0);
	}

	[Fact]
	[Trait("Seeding", null)]
	public void A_missing_attribute_publishes_zero_rather_than_throwing()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		Effect effect = CreateDrain(
			target,
			tallyTag,
			AccumulationPolicy.Losses,
			"TestAttributeSet.NotAnAttribute",
			-5);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		target.EffectsManager.UpdateEffects(1);

		TallyOf(handle).Should().Be(0);
		PublishedTally(effect, tallyTag).Should().Be(0);
	}

	[Fact]
	[Trait("Instant", null)]
	public void An_instant_effect_tallies_its_single_execution()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		// An instant effect never becomes active, so the baseline is taken in OnEffectApplied instead.
		var effectData = new EffectData(
			"Strike",
			new DurationData(DurationType.Instant),
			[Flat(FullAttribute, -12)],
			effectComponents: [new AttributeAccumulatorEffectComponent(FullAttribute, tallyTag)]);

		var effect = new Effect(effectData, new EffectOwnership(target, target));

		target.EffectsManager.ApplyEffect(effect).Should().BeNull();

		PublishedTally(effect, tallyTag).Should().Be(12);
	}

	[Fact]
	[Trait("Stackable", null)]
	public void A_stack_application_does_not_reset_the_total()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		Tag tallyTag = TallyTag();

		EffectData effectData = CreateDrainData(tallyTag, AccumulationPolicy.Losses, FullAttribute, -5, stackable: true);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)))!;

		// The second application adds a stack to the effect already running, and its OnEffectApplied reaches the same
		// component instance that is already tallying.
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)))
			.Should().BeSameAs(handle);

		handle.StackCount.Should().Be(2);

		// Five on the first application, then ten as the second stack executes at double magnitude.
		TallyOf(handle).Should().Be(15);
	}

	private static StackingData CreateStackingData()
	{
		return new StackingData(
			new ScalableInt(5),
			new ScalableInt(1),
			StackPolicy.AggregateBySource,
			StackLevelPolicy.SegregateLevels,
			StackMagnitudePolicy.Sum,
			StackOverflowPolicy.DenyApplication,
			StackExpirationPolicy.ClearEntireStack,
			ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
			ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication,
			ExecuteOnSuccessfulApplication: true);
	}

	private static Modifier Flat(StringKey attribute, float magnitude)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)));
	}

	private static float TallyOf(ActiveEffectHandle handle)
	{
		return handle.GetComponent<AttributeAccumulatorEffectComponent>()!.Total;
	}

	private static float PublishedTally(Effect effect, Tag tallyTag)
	{
		return effect.DataTag[tallyTag];
	}

	private static IEnumerable<float> TalliesOf(ActiveEffectHandle handle)
	{
		return handle.ComponentInstances
			.OfType<AttributeAccumulatorEffectComponent>()
			.Select(x => x.Total);
	}

	private static EffectData CreateDrainData(
		Tag tallyTag,
		AccumulationPolicy policy,
		StringKey attribute,
		float magnitude,
		bool stackable = false)
	{
		return new EffectData(
			"Drain",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[Flat(attribute, magnitude)],
			stackingData: stackable ? CreateStackingData() : null,
			periodicData: Periodic,
			effectComponents: [new AttributeAccumulatorEffectComponent(attribute, tallyTag, policy)]);
	}

	private static Effect CreateDrain(
		TestEntity target,
		Tag tallyTag,
		AccumulationPolicy policy,
		StringKey attribute,
		float magnitude)
	{
		return new Effect(
			CreateDrainData(tallyTag, policy, attribute, magnitude),
			new EffectOwnership(target, target));
	}

	private Tag TallyTag()
	{
		return Tag.RequestTag(_tagsManager, "tag");
	}
}
