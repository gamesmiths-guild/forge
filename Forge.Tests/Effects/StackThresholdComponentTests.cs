// Copyright © Gamesmiths Guild.

using FluentAssertions;
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

public class StackThresholdComponentTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Threshold", null)]
	public void The_threshold_effect_lands_only_once_the_count_reaches_it()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 3);

		ApplyStack(target, bleedData);
		HasHemorrhage(target).Should().BeFalse();

		ApplyStack(target, bleedData);
		HasHemorrhage(target).Should().BeFalse();

		ApplyStack(target, bleedData);
		HasHemorrhage(target).Should().BeTrue();
	}

	[Fact]
	[Trait("Threshold", null)]
	public void Climbing_past_the_threshold_does_not_apply_it_again()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2);

		ApplyStack(target, bleedData);
		ApplyStack(target, bleedData);
		ApplyStack(target, bleedData);
		ApplyStack(target, bleedData);

		HemorrhagesOn(target).Should().ContainSingle();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void The_threshold_effect_is_taken_back_when_the_count_drops()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 3);

		ApplyStack(target, bleedData);
		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		HasHemorrhage(target).Should().BeTrue();

		// Removing a single stack drops the count to two, which no longer meets the threshold.
		target.EffectsManager.RemoveEffect(bleed, 1);

		bleed.StackCount.Should().Be(2);
		HasHemorrhage(target).Should().BeFalse();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void The_threshold_effect_can_be_earned_again_after_falling_below()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2);

		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		HasHemorrhage(target).Should().BeTrue();

		target.EffectsManager.RemoveEffect(bleed, 1);
		HasHemorrhage(target).Should().BeFalse();

		ApplyStack(target, bleedData);
		HasHemorrhage(target).Should().BeTrue();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void The_threshold_effect_goes_with_its_applier()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2);

		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		HasHemorrhage(target).Should().BeTrue();

		target.EffectsManager.RemoveEffect(bleed, true);

		HasHemorrhage(target).Should().BeFalse();
	}

	[Fact]
	[Trait("Ignore", null)]
	public void An_ignored_threshold_effect_is_left_alone_when_the_count_drops()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2, policy: ConditionalEffectRemovalPolicy.Ignore);

		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		HasHemorrhage(target).Should().BeTrue();

		target.EffectsManager.RemoveEffect(bleed, 1);
		HasHemorrhage(target).Should().BeTrue();

		// Not even the applier's own removal takes it back; it lives by its own duration from here.
		target.EffectsManager.RemoveEffect(bleed, true);
		HasHemorrhage(target).Should().BeTrue();
	}

	[Fact]
	[Trait("Ignore", null)]
	public void An_ignored_threshold_effect_never_fires_a_second_time()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2, policy: ConditionalEffectRemovalPolicy.Ignore);

		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		target.EffectsManager.RemoveEffect(bleed, 1);
		ApplyStack(target, bleedData);

		HemorrhagesOn(target).Should().ContainSingle();
	}

	[Fact]
	[Trait("Threshold", null)]
	public void An_initial_stack_count_that_already_meets_the_threshold_crosses_on_arrival()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		// Three stacks land at once, so the threshold is met before the effect has changed at all.
		EffectData bleedData = CreateBleedData(threshold: 3, initialStack: 3);

		ApplyStack(target, bleedData);

		HasHemorrhage(target).Should().BeTrue();
	}

	[Fact]
	[Trait("Target", null)]
	public void The_threshold_effect_can_be_pointed_at_the_source()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		EffectData bleedData = CreateBleedData(threshold: 2, target: EffectApplicationTarget.Source);

		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(target, source)));
		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(target, source)));

		HasHemorrhage(source).Should().BeTrue();
		HasHemorrhage(target).Should().BeFalse();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Every_entry_is_applied_when_the_threshold_is_reached()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		EffectData bleedData = CreateBleedData(
			2,
			[
				new ConditionalEffect(CreateTaggedData("color.dark.red")),
				new ConditionalEffect(CreateTaggedData("color.red"))
			]);

		ApplyStack(target, bleedData);
		EffectsTagged(target, "color.dark.red").Should().BeEmpty();

		ApplyStack(target, bleedData);

		EffectsTagged(target, "color.dark.red").Should().ContainSingle();
		EffectsTagged(target, "color.red").Should().ContainSingle();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void Entries_keep_their_own_removal_policies()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		// One entry sustained by the count, one that just fires and is left alone.
		EffectData bleedData = CreateBleedData(
			2,
			[
				new ConditionalEffect(
					CreateTaggedData("color.dark.red"),
					RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd),
				new ConditionalEffect(CreateTaggedData("color.red"))
			]);

		ApplyStack(target, bleedData);
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		EffectsTagged(target, "color.dark.red").Should().ContainSingle();
		EffectsTagged(target, "color.red").Should().ContainSingle();

		target.EffectsManager.RemoveEffect(bleed, 1);

		// The sustained one went with the condition; the other stayed.
		EffectsTagged(target, "color.dark.red").Should().BeEmpty();
		EffectsTagged(target, "color.red").Should().ContainSingle();
	}

	[Fact]
	[Trait("Multiple", null)]
	public void A_failed_entry_does_not_stop_the_others()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		// TestEntity carries color.green, so the first entry's condition can never be met.
		EffectData bleedData = CreateBleedData(
			2,
			[
				new ConditionalEffect(
					CreateTaggedData("color.dark.red"),
					new TagRequirements(RequiredTags: Container("color.red"))),
				new ConditionalEffect(CreateTaggedData("color.red"))
			]);

		ApplyStack(target, bleedData);
		ApplyStack(target, bleedData);

		EffectsTagged(target, "color.dark.red").Should().BeEmpty();
		EffectsTagged(target, "color.red").Should().ContainSingle();
	}

	[Fact]
	[Trait("Condition", null)]
	public void The_threshold_effect_is_gated_on_the_sources_tags()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		// TestEntity carries color.green, so requiring color.red is a condition its source cannot meet.
		EffectData gatedData = CreateBleedData(
			threshold: 2,
			sourceRequirements: new TagRequirements(
				RequiredTags: new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.red")])));

		ApplyStack(target, gatedData);
		ApplyStack(target, gatedData);

		// The count reached the threshold, but the condition on the entry refused it. This is the capability the shared
		// ConditionalEffect brings that a bare EffectData could not express.
		HasHemorrhage(target).Should().BeFalse();

		EffectData ungatedData = CreateBleedData(
			threshold: 2,
			sourceRequirements: new TagRequirements(
				RequiredTags: new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.green")])));

		ApplyStack(target, ungatedData);
		ApplyStack(target, ungatedData);

		HasHemorrhage(target).Should().BeTrue();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Only_the_configured_stacks_are_taken_back_when_the_count_drops()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var component = new StackThresholdEffectComponent(
			2,
			[
				new ConditionalEffect(
					CreateStackableHemorrhageData(),
					RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd,
					StacksToRemove: 1)
			]);

		var bleedData = new EffectData(
			"Bleed",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			stackingData: CreateStackingData(2),
			effectComponents: [component]);

		// Two stacks land at once, meeting the threshold, and the Hemorrhage arrives with two stacks of its own.
		ActiveEffectHandle bleed = ApplyStack(target, bleedData);

		ActiveEffectHandle hemorrhage = HemorrhagesOn(target).Should().ContainSingle().Subject;
		hemorrhage.StackCount.Should().Be(2);

		// Dropping to a single stack falls below the threshold.
		target.EffectsManager.RemoveEffect(bleed, 1);
		bleed.StackCount.Should().Be(1);

		// One stack taken rather than the whole effect, which a bare EffectData had no way to ask for.
		HemorrhagesOn(target).Should().ContainSingle();
		hemorrhage.StackCount.Should().Be(1);
	}

	[Fact]
	[Trait("Data", null)]
	public void The_threshold_effect_can_inherit_the_set_by_caller_magnitudes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var payloadTag = Tag.RequestTag(_tagsManager, "color.red");

		EffectData bleedData = CreateBleedData(
			threshold: 2,
			hemorrhageData: CreateSetByCallerHemorrhageData(payloadTag),
			copyDataFromOriginalEffect: true);

		var bleed = new Effect(bleedData, new EffectOwnership(target, target));
		bleed.SetSetByCallerMagnitude(payloadTag, -25);

		target.EffectsManager.ApplyEffect(bleed);
		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(target, target)));

		HasHemorrhage(target).Should().BeTrue();

		// The threshold effect resolved the tag to the value set on the bleed that applied it.
		target.Attributes[TargetAttribute].CurrentValue.Should().Be(65);
	}

	[Fact]
	[Trait("Isolation", null)]
	public void Each_application_tracks_its_own_threshold_effect()
	{
		var firstSource = new TestEntity(_tagsManager, _cuesManager);
		var secondSource = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		// AggregateBySource keys on the ownership owner, so the two attackers' bleeds stay apart and each reaches its
		// own threshold on its own count.
		EffectData bleedData = CreateBleedData(threshold: 2);

		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(firstSource, firstSource)));
		ActiveEffectHandle first = target.EffectsManager.ApplyEffect(
			new Effect(bleedData, new EffectOwnership(firstSource, firstSource)))!;

		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(secondSource, secondSource)));
		target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(secondSource, secondSource)));

		HemorrhagesOn(target).Should().HaveCount(2);

		// Taking the first attacker's bleed away leaves the second attacker's hemorrhage standing.
		target.EffectsManager.RemoveEffect(first, true);

		HemorrhagesOn(target).Should().ContainSingle();
	}

	private static StackingData CreateStackingData(int initialStack)
	{
		return new StackingData(
			new ScalableInt(10),
			new ScalableInt(initialStack),
			StackPolicy.AggregateBySource,
			StackLevelPolicy.SegregateLevels,
			StackMagnitudePolicy.DontStack,
			StackOverflowPolicy.AllowApplication,
			StackExpirationPolicy.ClearEntireStack,
			ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication);
	}

	private static ActiveEffectHandle ApplyStack(TestEntity target, EffectData bleedData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(bleedData, new EffectOwnership(target, target)))!;
	}

	private static EffectData CreateBleedData(int threshold, ConditionalEffect[] thresholdEffects)
	{
		return new EffectData(
			"Bleed",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			stackingData: CreateStackingData(1),
			effectComponents: [new StackThresholdEffectComponent(threshold, thresholdEffects)]);
	}

	private EffectData CreateBleedData(
		int threshold,
		ConditionalEffectRemovalPolicy policy = ConditionalEffectRemovalPolicy.RemoveOnEnd,
		EffectApplicationTarget target = EffectApplicationTarget.Target,
		int initialStack = 1,
		EffectData? hemorrhageData = null,
		bool copyDataFromOriginalEffect = false,
		TagRequirements? sourceRequirements = null)
	{
		var component = new StackThresholdEffectComponent(
			threshold,
			[
				new ConditionalEffect(
					hemorrhageData ?? CreateHemorrhageData(),
					sourceRequirements,
					policy,
					Target: target)
			],
			copyDataFromOriginalEffect);

		return new EffectData(
			"Bleed",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			stackingData: CreateStackingData(initialStack),
			effectComponents: [component]);
	}

	private EffectData CreateTaggedData(string tagKey)
	{
		return new EffectData(
			$"Threshold Effect {tagKey}",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(Container(tagKey))]);
	}

	private TagContainer Container(string tagKey)
	{
		return new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, tagKey)]);
	}

	private IEnumerable<ActiveEffectHandle> EffectsTagged(TestEntity entity, string tagKey)
	{
		return entity.EffectsManager.GetActiveEffects(new EffectQuery(
			GrantedTagQuery: TagQuery.MakeQueryMatchAnyTags(Container(tagKey))));
	}

	private EffectData CreateStackableHemorrhageData()
	{
		return new EffectData(
			"Stacking Hemorrhage",
			new DurationData(DurationType.Infinite),
			stackingData: CreateStackingData(2),
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.dark.red")]))
			]);
	}

	private EffectData CreateHemorrhageData()
	{
		return new EffectData(
			"Hemorrhage",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.dark.red")]))
			]);
	}

	private EffectData CreateSetByCallerHemorrhageData(Tag payloadTag)
	{
		return new EffectData(
			"Hemorrhage",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(payloadTag)))
			],
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.dark.red")]))
			]);
	}

	private IEnumerable<ActiveEffectHandle> HemorrhagesOn(TestEntity entity)
	{
		return entity.EffectsManager.GetActiveEffects(new EffectQuery(
			GrantedTagQuery: TagQuery.MakeQueryMatchAnyTags(
				new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "color.dark.red")]))));
	}

	private bool HasHemorrhage(TestEntity entity)
	{
		return HemorrhagesOn(entity).Any();
	}
}
