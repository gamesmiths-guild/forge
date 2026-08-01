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
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class AdditionalEffectsComponentTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Application", null)]
	public void Application_applies_the_configured_effects()
	{
		TestEntity target = CreateEntity();

		Apply(target, CreateApplierData(onApplication: [Conditional("color.red"), Conditional("color.blue")]));

		HasEffect(target, "color.red").Should().BeTrue();
		HasEffect(target, "color.blue").Should().BeTrue();
	}

	[Fact]
	[Trait("Application", null)]
	public void An_instant_effect_applies_its_effects()
	{
		TestEntity target = CreateEntity();

		// Instant effects notify component instances rather than definitions, which is what lets a stateful component
		// like this one work from one at all.
		Apply(
			target,
			CreateApplierData([Conditional("color.red")], durationType: DurationType.Instant));

		HasEffect(target, "color.red").Should().BeTrue();
	}

	[Fact]
	[Trait("Application", null)]
	public void Each_stack_application_applies_the_effects_again()
	{
		TestEntity target = CreateEntity();

		EffectData applierData = CreateApplierData([Conditional("color.red")], stackable: true);

		ActiveEffectHandle applier = Apply(target, applierData)!;
		Apply(target, applierData).Should().BeSameAs(applier);

		applier.StackCount.Should().Be(2);
		EffectsOf(target, "color.red").Should().HaveCount(2);
	}

	[Fact]
	[Trait("Condition", null)]
	public void Conditions_are_evaluated_against_the_source_and_not_the_target()
	{
		TestEntity target = CreateEntity();
		TestEntity plainAttacker = CreateEntity();
		TestEntity venomousAttacker = CreateEntity();

		ApplyTagEffect(venomousAttacker, MakeContainer("enemy.beast.wolf"));

		// The target carries the tag as well, so a component reading the wrong end would let both through.
		ApplyTagEffect(target, MakeContainer("enemy.beast.wolf"));

		ConditionalEffect[] onApplication =
		[
			Conditional("color.red", new TagRequirements(RequiredTags: MakeContainer("enemy.beast.wolf")))
		];

		ApplyFrom(target, plainAttacker, CreateApplierData(onApplication));
		HasEffect(target, "color.red").Should().BeFalse();

		ApplyFrom(target, venomousAttacker, CreateApplierData(onApplication));
		HasEffect(target, "color.red").Should().BeTrue();
	}

	[Fact]
	[Trait("Condition", null)]
	public void Each_condition_is_evaluated_on_its_own()
	{
		TestEntity target = CreateEntity();

		// TestEntity starts out tagged color.green, so the first condition is met and the second is not.
		Apply(target, CreateApplierData(
		[
			Conditional("color.red", new TagRequirements(RequiredTags: MakeContainer("color.green"))),
			Conditional("color.blue", new TagRequirements(IgnoreTags: MakeContainer("color.green")))
		]));

		HasEffect(target, "color.red").Should().BeTrue();
		HasEffect(target, "color.blue").Should().BeFalse();
	}

	[Fact]
	[Trait("Target", null)]
	public void Effects_can_be_pointed_back_at_the_source()
	{
		TestEntity target = CreateEntity();
		TestEntity attacker = CreateEntity();

		// Thorns: what lands on the target recoils onto whoever caused it.
		ApplyFrom(target, attacker, CreateApplierData(
			[Conditional("color.red", applicationTarget: EffectApplicationTarget.Source)]));

		HasEffect(attacker, "color.red").Should().BeTrue();
		HasEffect(target, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("Target", null)]
	public void Effects_can_be_pointed_at_the_owner()
	{
		TestEntity target = CreateEntity();
		TestEntity owner = CreateEntity();
		TestEntity source = CreateEntity();

		target.EffectsManager.ApplyEffect(new Effect(
			CreateApplierData([Conditional("color.red", applicationTarget: EffectApplicationTarget.Owner)]),
			new EffectOwnership(owner, source)));

		HasEffect(owner, "color.red").Should().BeTrue();
		HasEffect(source, "color.red").Should().BeFalse();
		HasEffect(target, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("Target", null)]
	public void An_effect_pointed_at_an_entity_that_is_not_there_is_skipped()
	{
		TestEntity target = CreateEntity();

		Action act = () => target.EffectsManager.ApplyEffect(new Effect(
			CreateApplierData([Conditional("color.red", applicationTarget: EffectApplicationTarget.Source)]),
			new EffectOwnership(null, null)));

		// Nowhere to land is not the same as landing on the target instead.
		act.Should().NotThrow();
		HasEffect(target, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("Inheritance", null)]
	public void Applied_effects_credit_the_original_source()
	{
		TestEntity target = CreateEntity();
		TestEntity caster = CreateEntity();

		ApplyFrom(target, caster, CreateApplierData([Conditional("color.red")]));

		Effect? applied = EffectsOf(target, "color.red").Single().Effect;

		applied.Should().NotBeNull();
		applied!.Ownership.Source.Should().Be(caster);
		applied.Ownership.Owner.Should().Be(caster);
	}

	[Fact]
	[Trait("Inheritance", null)]
	public void Applied_effects_land_at_the_level_of_the_effect_that_applied_them()
	{
		TestEntity target = CreateEntity();

		// The applied effect's magnitude doubles at level 2, so the attribute reports the level it landed at.
		target.EffectsManager.ApplyEffect(new Effect(
			CreateApplierData([Conditional("color.red", scalesWithLevel: true)]),
			new EffectOwnership(target, target),
			level: 2));

		ActiveEffectHandle applied = EffectsOf(target, "color.red").Single();

		applied.Level.Should().Be(2);
		TestUtils.TestAttribute(target, TargetAttribute, [11, 1, 10, 0]);
	}

	[Fact]
	[Trait("Inheritance", null)]
	public void Set_by_caller_magnitudes_carry_over_when_copying_data()
	{
		TestEntity target = CreateEntity();
		var magnitudeTag = Tag.RequestTag(_tagsManager, "tag");

		var applier = new Effect(
			CreateApplierData(
				[Conditional("color.red", setByCallerTag: magnitudeTag)],
				copyDataFromOriginalEffect: true),
			new EffectOwnership(target, target));

		applier.SetSetByCallerMagnitude(magnitudeTag, 7);

		target.EffectsManager.ApplyEffect(applier);

		// The applied effect resolved the caller's value instead of failing to find one.
		TestUtils.TestAttribute(target, TargetAttribute, [8, 1, 7, 0]);
	}

	[Fact]
	[Trait("Inheritance", null)]
	public void Set_by_caller_magnitudes_stay_behind_by_default()
	{
		TestEntity target = CreateEntity();
		var magnitudeTag = Tag.RequestTag(_tagsManager, "tag");

		var applier = new Effect(
			CreateApplierData([Conditional("color.red")]),
			new EffectOwnership(target, target));

		applier.SetSetByCallerMagnitude(magnitudeTag, 7);

		target.EffectsManager.ApplyEffect(applier);

		ActiveEffectHandle applied = EffectsOf(target, "color.red").Single();

		applied.Effect!.DataTag.Should().BeEmpty();
	}

	[Fact]
	[Trait("Inheritance", null)]
	public void The_carried_magnitudes_are_a_copy_and_not_a_shared_dictionary()
	{
		TestEntity target = CreateEntity();
		var magnitudeTag = Tag.RequestTag(_tagsManager, "tag");

		var applier = new Effect(
			CreateApplierData([Conditional("color.red")], copyDataFromOriginalEffect: true),
			new EffectOwnership(target, target));

		applier.SetSetByCallerMagnitude(magnitudeTag, 7);

		target.EffectsManager.ApplyEffect(applier);

		applier.SetSetByCallerMagnitude(magnitudeTag, 9);

		ActiveEffectHandle applied = EffectsOf(target, "color.red").Single();

		applied.Effect!.DataTag.Should().Contain(magnitudeTag, 7);
	}

	[Fact]
	[Trait("Completion", null)]
	public void Running_out_of_duration_fires_the_normal_completion_effects()
	{
		TestEntity target = CreateEntity();

		Apply(target, CreateApplierData(
			onCompleteNormal: [Conditional("color.red")],
			onCompletePrematurely: [Conditional("color.blue")],
			durationType: DurationType.HasDuration));

		target.EffectsManager.UpdateEffects(10);

		HasEffect(target, "color.red").Should().BeTrue();
		HasEffect(target, "color.blue").Should().BeFalse();
	}

	[Fact]
	[Trait("Completion", null)]
	public void Being_taken_away_early_fires_the_premature_completion_effects()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
			onCompleteNormal: [Conditional("color.red")],
			onCompletePrematurely: [Conditional("color.blue")],
			durationType: DurationType.HasDuration))!;

		target.EffectsManager.RemoveEffect(applier, true);

		HasEffect(target, "color.red").Should().BeFalse();
		HasEffect(target, "color.blue").Should().BeTrue();
	}

	[Theory]
	[Trait("Completion", null)]
	[InlineData(true)]
	[InlineData(false)]
	public void The_always_completion_effects_fire_either_way(bool letItExpire)
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
			onCompleteAlways: [Conditional("color.red")],
			durationType: DurationType.HasDuration))!;

		if (letItExpire)
		{
			target.EffectsManager.UpdateEffects(10);
		}
		else
		{
			target.EffectsManager.RemoveEffect(applier, true);
		}

		HasEffect(target, "color.red").Should().BeTrue();
	}

	[Fact]
	[Trait("Completion", null)]
	public void An_infinite_effect_removed_by_hand_counts_as_premature()
	{
		TestEntity target = CreateEntity();

		// Infinite effects have no natural end, so every removal of one is premature.
		ActiveEffectHandle applier = Apply(target, CreateApplierData(
			onCompleteNormal: [Conditional("color.red")],
			onCompletePrematurely: [Conditional("color.blue")]))!;

		target.EffectsManager.RemoveEffect(applier, true);

		HasEffect(target, "color.red").Should().BeFalse();
		HasEffect(target, "color.blue").Should().BeTrue();
	}

	[Fact]
	[Trait("Completion", null)]
	public void Completion_effects_can_be_pointed_back_at_the_source()
	{
		TestEntity target = CreateEntity();
		TestEntity caster = CreateEntity();

		// The curse shape: while it runs it works on its victim, and when it ends it pays its caster back.
		ActiveEffectHandle curse = ApplyFrom(target, caster, CreateApplierData(
			onCompleteAlways: [Conditional("color.red", applicationTarget: EffectApplicationTarget.Source)],
			durationType: DurationType.HasDuration))!;

		HasEffect(caster, "color.red").Should().BeFalse();

		target.EffectsManager.RemoveEffect(curse, true);

		HasEffect(caster, "color.red").Should().BeTrue();
		HasEffect(target, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("Completion", null)]
	public void Completion_effects_are_gated_on_the_source_tags_too()
	{
		TestEntity target = CreateEntity();
		TestEntity plainCaster = CreateEntity();
		TestEntity venomousCaster = CreateEntity();

		ApplyTagEffect(venomousCaster, MakeContainer("enemy.beast.wolf"));

		ConditionalEffect[] onCompleteAlways =
		[
			Conditional("color.red", new TagRequirements(RequiredTags: MakeContainer("enemy.beast.wolf")))
		];

		ActiveEffectHandle plain = ApplyFrom(
			target,
			plainCaster,
			CreateApplierData(onCompleteAlways: onCompleteAlways))!;
		target.EffectsManager.RemoveEffect(plain, true);

		HasEffect(target, "color.red").Should().BeFalse();

		ActiveEffectHandle venomous = ApplyFrom(
			target,
			venomousCaster,
			CreateApplierData(onCompleteAlways: onCompleteAlways))!;
		target.EffectsManager.RemoveEffect(venomous, true);

		HasEffect(target, "color.red").Should().BeTrue();
	}

	[Fact]
	[Trait("Completion", null)]
	public void A_completion_effect_reads_the_magnitudes_the_effect_ended_with()
	{
		TestEntity target = CreateEntity();
		TestEntity caster = CreateEntity();
		var magnitudeTag = Tag.RequestTag(_tagsManager, "tag");

		// What a damage tally would do: accumulate while the effect runs, and let the completion effect read the
		// total. The copy happens at removal, so the last value written is the one that carries.
		var curse = new Effect(
			CreateApplierData(
				onCompleteAlways:
				[
					Conditional(
						"color.red",
						applicationTarget: EffectApplicationTarget.Source,
						setByCallerTag: magnitudeTag)
				],
				copyDataFromOriginalEffect: true),
			new EffectOwnership(caster, caster));

		curse.SetSetByCallerMagnitude(magnitudeTag, 3);

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(curse)!;

		curse.SetSetByCallerMagnitude(magnitudeTag, 12);

		target.EffectsManager.RemoveEffect(handle, true);

		// The heal landed on the caster at the tally's final value, not the one it started with.
		TestUtils.TestAttribute(caster, TargetAttribute, [13, 1, 12, 0]);
	}

	[Fact]
	[Trait("Completion", null)]
	public void Losing_a_stack_is_not_an_ending()
	{
		TestEntity target = CreateEntity();

		EffectData applierData = CreateApplierData(
			onCompleteAlways: [Conditional("color.red")],
			stackable: true);

		ActiveEffectHandle applier = Apply(target, applierData)!;
		Apply(target, applierData);

		applier.StackCount.Should().Be(2);

		target.EffectsManager.RemoveEffect(applier, 1);

		applier.StackCount.Should().Be(1);
		HasEffect(target, "color.red").Should().BeFalse();

		target.EffectsManager.RemoveEffect(applier, 1);

		applier.IsValid.Should().BeFalse();
		HasEffect(target, "color.red").Should().BeTrue();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_effects_go_when_the_effect_that_applied_them_goes()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
		[
			Conditional("color.red", removalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd),
			Conditional("color.blue")
		]))!;

		HasEffect(target, "color.red").Should().BeTrue();
		HasEffect(target, "color.blue").Should().BeTrue();

		target.EffectsManager.RemoveEffect(applier, true);

		HasEffect(target, "color.red").Should().BeFalse();
		HasEffect(target, "color.blue").Should().BeTrue();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_effects_also_go_when_the_effect_expires()
	{
		TestEntity target = CreateEntity();

		Apply(target, CreateApplierData(
			[Conditional("color.red", removalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)],
			durationType: DurationType.HasDuration));

		HasEffect(target, "color.red").Should().BeTrue();

		target.EffectsManager.UpdateEffects(10);

		HasEffect(target, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_takes_only_the_configured_number_of_stacks()
	{
		TestEntity target = CreateEntity();

		EffectData stackableData = CreateAppliedData("color.red", stackable: true);

		Apply(target, stackableData);
		Apply(target, stackableData);
		ActiveEffectHandle applied = Apply(target, stackableData)!;

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
		[
			new ConditionalEffect(
				stackableData,
				RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd,
				StacksToRemove: 2)
		]))!;

		// The applier's own application stacked onto what was already there.
		applied.StackCount.Should().Be(4);

		target.EffectsManager.RemoveEffect(applier, true);

		applied.IsValid.Should().BeTrue();
		applied.StackCount.Should().Be(2);
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_reaches_an_effect_it_applied_to_another_entity()
	{
		TestEntity target = CreateEntity();
		TestEntity attacker = CreateEntity();

		ActiveEffectHandle applier = ApplyFrom(target, attacker, CreateApplierData(
		[
			Conditional(
				"color.red",
				removalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd,
				applicationTarget: EffectApplicationTarget.Source)
		]))!;

		HasEffect(attacker, "color.red").Should().BeTrue();

		target.EffectsManager.RemoveEffect(applier, true);

		HasEffect(attacker, "color.red").Should().BeFalse();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void Remove_on_end_takes_back_what_it_applied_and_not_every_effect_like_it()
	{
		TestEntity target = CreateEntity();

		EffectData appliedData = CreateAppliedData("color.red");

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
		[
			new ConditionalEffect(appliedData, RemovalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)
		]))!;

		// The same effect, from somewhere else entirely.
		ActiveEffectHandle bystander = Apply(target, appliedData)!;

		EffectsOf(target, "color.red").Should().HaveCount(2);

		target.EffectsManager.RemoveEffect(applier, true);

		// Handles are tracked, not definitions.
		bystander.IsValid.Should().BeTrue();
		EffectsOf(target, "color.red").Should().ContainSingle();
	}

	[Fact]
	[Trait("RemoveOnEnd", null)]
	public void An_applied_effect_that_is_already_gone_is_left_alone()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle applier = Apply(target, CreateApplierData(
			[Conditional("color.red", removalPolicy: ConditionalEffectRemovalPolicy.RemoveOnEnd)]))!;

		ActiveEffectHandle applied = EffectsOf(target, "color.red").Single();
		target.EffectsManager.RemoveEffect(applied, true);

		Action act = () => target.EffectsManager.RemoveEffect(applier, true);

		act.Should().NotThrow();
	}

	private static ActiveEffectHandle? Apply(TestEntity target, EffectData effectData)
	{
		return ApplyFrom(target, target, effectData);
	}

	private static ActiveEffectHandle? ApplyFrom(TestEntity target, TestEntity source, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)));
	}

	private static void ApplyTagEffect(TestEntity entity, TagContainer tags)
	{
		var tagEffectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(tags)]);

		entity.EffectsManager.ApplyEffect(new Effect(tagEffectData, new EffectOwnership(entity, entity)));
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
			StackExpirationPolicy.ClearEntireStack);
	}

	private static Modifier[] CreateModifiers(bool scalesWithLevel, Tag? setByCallerTag)
	{
		if (setByCallerTag.HasValue)
		{
			return
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(setByCallerTag.Value)))
			];
		}

		// Doubling at level 2 is what makes the level an applied effect landed at readable from the attribute.
		ICurve? scalingCurve = scalesWithLevel ? new Curve([new CurveKey(1, 1), new CurveKey(2, 2)]) : null;

		return
		[
			new Modifier(
				TargetAttribute,
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5, scalingCurve)))
		];
	}

	private static DurationData CreateDurationData(DurationType durationType)
	{
		return durationType == DurationType.HasDuration
			? new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f)))
			: new DurationData(durationType);
	}

	private static EffectData CreateApplierData(
		ConditionalEffect[]? onApplication = null,
		ConditionalEffect[]? onCompleteAlways = null,
		ConditionalEffect[]? onCompleteNormal = null,
		ConditionalEffect[]? onCompletePrematurely = null,
		bool copyDataFromOriginalEffect = false,
		DurationType durationType = DurationType.Infinite,
		bool stackable = false)
	{
		return new EffectData(
			"Applier",
			CreateDurationData(durationType),
			stackingData: stackable ? CreateStackingData() : null,
			effectComponents:
			[
				new AdditionalEffectsEffectComponent(
					onApplication,
					onCompleteAlways,
					onCompleteNormal,
					onCompletePrematurely,
					copyDataFromOriginalEffect)
			]);
	}

	private IEnumerable<ActiveEffectHandle> EffectsOf(TestEntity entity, string effectTagKey)
	{
		return entity.EffectsManager.GetActiveEffects(MakeQuery(effectTagKey));
	}

	private bool HasEffect(TestEntity entity, string effectTagKey)
	{
		return entity.EffectsManager.HasAnyActiveEffect(MakeQuery(effectTagKey));
	}

	private EffectQuery MakeQuery(string effectTagKey)
	{
		return new EffectQuery(EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer(effectTagKey)));
	}

	private ConditionalEffect Conditional(
		string effectTagKey,
		TagRequirements? sourceTagRequirements = null,
		ConditionalEffectRemovalPolicy removalPolicy = ConditionalEffectRemovalPolicy.Ignore,
		EffectApplicationTarget applicationTarget = EffectApplicationTarget.Target,
		bool scalesWithLevel = false,
		Tag? setByCallerTag = null)
	{
		return new ConditionalEffect(
			CreateAppliedData(effectTagKey, scalesWithLevel: scalesWithLevel, setByCallerTag: setByCallerTag),
			sourceTagRequirements,
			removalPolicy,
			Target: applicationTarget);
	}

	private EffectData CreateAppliedData(
		string effectTagKey,
		bool stackable = false,
		bool scalesWithLevel = false,
		Tag? setByCallerTag = null)
	{
		return new EffectData(
			$"Applied {effectTagKey}",
			new DurationData(DurationType.Infinite),
			CreateModifiers(scalesWithLevel, setByCallerTag),
			stackable ? CreateStackingData() : null,
			effectTags: MakeContainer(effectTagKey));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}
}
