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
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

/// <summary>
/// End-to-end check of a curse that damages its victim over time and, when it ends, heals its caster for the total it
/// dealt. It is built entirely from shipped parts — a periodic effect,
/// <see cref="AttributeAccumulatorEffectComponent"/> to tally, and <see cref="AdditionalEffectsEffectComponent"/> to
/// pay out. Nothing here is a special case in the library, and nothing here is a custom component.
/// </summary>
/// <remarks>
/// The last two cases are why the tally is worth shipping as a component: they pin down the two ways the obvious
/// hand-rolled version — summing <see cref="EffectEvaluatedData.ModifiersEvaluatedData"/> — gets it wrong.
/// </remarks>
/// <param name="tagsAndCuesFixture">The fixture providing tags and cues managers for the test.</param>
public class CurseSiphonScenarioTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string VictimAttribute = "TestAttributeSet.Attribute90";
	private const string CasterAttribute = "TestAttributeSet.Attribute1000";

	private const int DamagePerTick = 5;

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Scenario", "Curse")]
	public void A_curse_that_expires_heals_its_caster_for_everything_it_dealt()
	{
		var caster = new TestEntity(_tagsManager, _cuesManager);
		var victim = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle curse = ApplyCurse(caster, victim);

		// Ticks on application and once per second after, for a ten second duration.
		for (int i = 0; i < 10; i++)
		{
			victim.EffectsManager.UpdateEffects(1);
		}

		curse.IsValid.Should().BeFalse();

		// Once on application and once per second, the last of them landing as the duration runs out.
		const int totalDealt = 11 * DamagePerTick;

		TestUtils.TestAttribute(victim, VictimAttribute, [90 - totalDealt, 90 - totalDealt, 0, 0]);

		// What the caster gains is exactly what the victim lost, which is the whole point of the tally.
		TestUtils.TestAttribute(caster, CasterAttribute, [totalDealt, totalDealt, 0, 0]);
	}

	[Fact]
	[Trait("Scenario", "Curse")]
	public void A_curse_dispelled_early_pays_out_only_what_it_managed_to_deal()
	{
		var caster = new TestEntity(_tagsManager, _cuesManager);
		var victim = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle curse = ApplyCurse(caster, victim);

		victim.EffectsManager.UpdateEffects(1);
		victim.EffectsManager.UpdateEffects(1);

		// Cleansed after three ticks: the one on application plus two.
		victim.EffectsManager.RemoveEffect(curse, true);

		const int totalDealt = 3 * DamagePerTick;

		TestUtils.TestAttribute(victim, VictimAttribute, [90 - totalDealt, 90 - totalDealt, 0, 0]);
		TestUtils.TestAttribute(caster, CasterAttribute, [totalDealt, totalDealt, 0, 0]);
	}

	[Fact]
	[Trait("Scenario", "Curse")]
	public void Two_curses_on_the_same_victim_pay_their_own_casters_their_own_totals()
	{
		var firstCaster = new TestEntity(_tagsManager, _cuesManager);
		var secondCaster = new TestEntity(_tagsManager, _cuesManager);
		var victim = new TestEntity(_tagsManager, _cuesManager);

		ActiveEffectHandle first = ApplyCurse(firstCaster, victim);

		victim.EffectsManager.UpdateEffects(1);

		// The second curse arrives a tick late, so it has one fewer tick to its name.
		ActiveEffectHandle second = ApplyCurse(secondCaster, victim);

		victim.EffectsManager.UpdateEffects(1);

		victim.EffectsManager.RemoveEffect(first, true);
		victim.EffectsManager.RemoveEffect(second, true);

		// Each tally belongs to its own application, which is what CreateInstance buys and what an attribute on the
		// victim could never do. Neither one counts the other's damage either, because the baseline each measures
		// against absorbs every change it did not cause.
		TestUtils.TestAttribute(firstCaster, CasterAttribute, [3 * DamagePerTick, 3 * DamagePerTick, 0, 0]);
		TestUtils.TestAttribute(secondCaster, CasterAttribute, [2 * DamagePerTick, 2 * DamagePerTick, 0, 0]);
	}

	[Fact]
	[Trait("Scenario", "Curse")]
	public void The_payout_counts_what_the_victim_could_absorb_and_not_what_was_aimed_at_it()
	{
		var caster = new TestEntity(_tagsManager, _cuesManager);
		var victim = new TestEntity(_tagsManager, _cuesManager);
		var damageDealtTag = Tag.RequestTag(_tagsManager, "tag");

		// Attribute5 holds 5, and the curse aims 5 per tick at it. The second tick lands on an empty pool.
		ActiveEffectHandle curse = victim.EffectsManager.ApplyEffect(new Effect(
			CreateCurseData(damageDealtTag, "TestAttributeSet.Attribute5"),
			new EffectOwnership(caster, caster)))!;

		victim.EffectsManager.UpdateEffects(1);

		victim.EffectsManager.RemoveEffect(curse, true);

		TestUtils.TestAttribute(victim, "TestAttributeSet.Attribute5", [0, 0, 0, 0]);

		// Two ticks of 5 were aimed, but only 5 landed, so only 5 is paid out. Summing ModifiersEvaluatedData would
		// have tallied 10 and refunded the overkill.
		TestUtils.TestAttribute(caster, CasterAttribute, [5, 5, 0, 0]);
	}

	[Fact]
	[Trait("Scenario", "Curse")]
	public void The_payout_counts_damage_a_custom_execution_produced()
	{
		var caster = new TestEntity(_tagsManager, _cuesManager);
		var victim = new TestEntity(_tagsManager, _cuesManager);
		var damageDealtTag = Tag.RequestTag(_tagsManager, "tag");

		// Where a resistance formula usually lives: the curse declares no damage modifier of its own, and all of it
		// comes out of the execution. Execution output never reaches ModifiersEvaluatedData, so summing that array
		// would have tallied nothing at all.
		var curseData = new EffectData(
			"Executed Curse",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			periodicData: new PeriodicData(
				new ScalableFloat(1),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(VictimAttribute, damageDealtTag),
				new AdditionalEffectsEffectComponent(
					onCompleteAlways:
					[
						new ConditionalEffect(
							CreateSiphonData(damageDealtTag),
							Target: EffectApplicationTarget.Source)
					],
					copyDataFromOriginalEffect: true)
			],
			customExecutions: [new ResistedDamageExecution()]);

		ActiveEffectHandle curse = victim.EffectsManager.ApplyEffect(
			new Effect(curseData, new EffectOwnership(caster, caster)))!;

		victim.EffectsManager.RemoveEffect(curse, true);

		TestUtils.TestAttribute(victim, VictimAttribute, [83, 83, 0, 0]);
		TestUtils.TestAttribute(caster, CasterAttribute, [7, 7, 0, 0]);
	}

	private static EffectData CreateSiphonData(Tag damageDealtTag)
	{
		// The payout reads its magnitude from the tally rather than from a fixed number.
		return new EffectData(
			"Curse Siphon",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					CasterAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(damageDealtTag)))
			]);
	}

	private static EffectData CreateCurseData(Tag damageDealtTag, StringKey victimAttribute)
	{
		return new EffectData(
			"Curse",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[
				new Modifier(
					victimAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-DamagePerTick)))
			],
			periodicData: new PeriodicData(
				new ScalableFloat(1),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents:
			[
				new AttributeAccumulatorEffectComponent(victimAttribute, damageDealtTag),
				new AdditionalEffectsEffectComponent(
					onCompleteAlways:
					[
						new ConditionalEffect(
							CreateSiphonData(damageDealtTag),
							Target: EffectApplicationTarget.Source)
					],
					copyDataFromOriginalEffect: true)
			]);
	}

	private ActiveEffectHandle ApplyCurse(TestEntity caster, TestEntity victim)
	{
		var damageDealtTag = Tag.RequestTag(_tagsManager, "tag");

		// A fresh Effect per cast: the tally is published on the Effect, so reusing one would merge the casters'
		// totals.
		ActiveEffectHandle? handle = victim.EffectsManager.ApplyEffect(
			new Effect(CreateCurseData(damageDealtTag, VictimAttribute), new EffectOwnership(caster, caster)));

		handle.Should().NotBeNull();

		return handle!;
	}

	/// <summary>
	/// Stands in for a resistance formula: damage resolved at execution time rather than declared as a modifier.
	/// </summary>
	private sealed class ResistedDamageExecution : CustomExecution
	{
		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			return
			[
				new ModifierEvaluatedData(
					target.Attributes[VictimAttribute],
					ModifierOperation.FlatBonus,
					-7)
			];
		}
	}
}
