// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// An entity snapshot puts the entity back silently: attributes, tags, effects, abilities, their graphs, shared
/// variables, input, random source and body, with effects, abilities and instances back as the very objects their
/// handles point at, and everything that holds on to something outside itself taking hold again in its first order.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class EntitySnapshotTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private const string Attribute1 = "TestAttributeSet.Attribute1";

	private const string Attribute90 = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restore_puts_attributes_back_with_their_modifiers_and_raises_nothing()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EntityReplay.Watch(entity, log);
		ActiveEffectHandle buff = Apply(entity, Lasting("Buff", DurationType.Infinite, Flat(Attribute1, 10)))!;
		Apply(entity, Lasting("Override", DurationType.Infinite, Override(Attribute90, 50, channel: 1)));

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		string captured = EntityState.Describe(entity);

		Apply(entity, Instant("Hit", Attribute1, -1));
		Apply(entity, Lasting("Percent", DurationType.Infinite, Percent(Attribute1, 1)));
		entity.EffectsManager.RemoveEffect(buff);
		log.Clear();

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		EntityState.Describe(entity).Should().Be(captured);
		log.Should().BeEmpty("a restore raises nothing");

		entity.EffectsManager.RemoveEffect(buff);

		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(
			1,
			"the buff's modifier came back in its channel, so taking it away again leaves the base");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_set_added_since_the_snapshot_leaves_without_the_modifiers_its_effects_gave_it()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var vitals = new VitalAttributeSet();
		Apply(entity, Lasting("Buff", DurationType.Infinite, Flat("VitalAttributeSet.Vitality", 10)));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.Attributes.AddAttributeSet(vitals);
		vitals.Vitality.CurrentValue.Should().Be(20);

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		entity.Attributes.AttributeSets.Should().NotContain(vitals);
		vitals.Vitality.CurrentValue.Should().Be(10, "the set left without the buff's modifier");

		entity.Attributes.AddAttributeSet(vitals);

		vitals.Vitality.CurrentValue.Should().Be(20, "adding the set again brings the buff's modifier only once");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restore_puts_tags_back_and_raises_nothing()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle blue = Apply(entity, Tagged("Blue", "color.blue"))!;
		EntityReplay.Watch(entity, log);

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		string captured = EntityState.Describe(entity);

		Apply(entity, Tagged("Red", "color.red"));
		Apply(entity, Tagged("Blue again", "color.blue"));
		log.Clear();

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		EntityState.Describe(entity).Should().Be(captured);
		log.Should().BeEmpty();
		entity.Tags.AllTags.HasTag(Tag.RequestTag(_tagsManager, "color.red")).Should().BeFalse();

		entity.EffectsManager.RemoveEffect(blue);

		entity.Tags.AllTags.HasTag(Tag.RequestTag(_tagsManager, "color.blue")).Should().BeFalse(
			"the blue tag counts one application again, so removing it takes the tag away");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void An_effect_applied_since_the_snapshot_goes_with_its_handle()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		ActiveEffectHandle late = Apply(entity, Lasting("Late", DurationType.Infinite, Flat(Attribute1, 10)))!;
		EntityReplay.Watch(entity, log);

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		late.IsValid.Should().BeFalse();
		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1);
		log.Should().BeEmpty("the effect goes without a removal");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void An_effect_removed_since_the_snapshot_comes_back_as_the_same_handle()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle buff = Apply(entity, Lasting("Buff", Seconds(1), Flat(Attribute1, 10)))!;
		entity.EffectsManager.UpdateEffects(0.25);

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		entity.EffectsManager.RemoveEffect(buff);
		buff.IsValid.Should().BeFalse();

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		buff.IsValid.Should().BeTrue();
		buff.RemainingDuration.Should().Be(0.75);
		entity.EffectsManager.GetActiveEffects().Should().ContainSingle().Which.Should().BeSameAs(buff);
		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(11);
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restored_effect_carries_on_with_its_stacks_level_timing_and_set_by_caller_values()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var magnitude = Tag.RequestTag(_tagsManager, "simple.tag");
		var effect = new Effect(Stacking("Poison"), new EffectOwnership(entity, entity));
		effect.SetSetByCallerMagnitude(magnitude, 1);
		entity.EffectsManager.ApplyEffect(effect);
		entity.EffectsManager.UpdateEffects(0.125);

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		string captured = EntityState.Describe(entity);
		string carriedOn = CarryOn(entity, effect);

		effect.SetLevel(3);
		effect.SetSetByCallerMagnitude(magnitude, 9);
		entity.EffectsManager.ApplyEffect(effect);
		entity.EffectsManager.UpdateEffects(0.3);

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		EntityState.Describe(entity).Should().Be(captured);
		effect.Level.Should().Be(1);
		effect.DataTag.Should().Contain(magnitude, 1);

		CarryOn(entity, effect)
			.Should().Be(carriedOn, "the restored effect runs on exactly as it did from the snapshot");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restored_effect_watches_its_tags_again()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle ward = Apply(entity, Ward("Ward"))!;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.EffectsManager.RemoveEffect(ward);
		Apply(entity, Ward("Late ward"));
		EntitySimulation.RestoreSnapshot(entity, snapshot);
		EntityReplay.Watch(entity, log);

		Apply(entity, Tagged("Red", "color.red"));

		ward.IsInhibited.Should().BeTrue("the restored ward took hold of the tags again");
		log.Should().ContainSingle(x => x.StartsWith("changed", StringComparison.Ordinal))
			.Which.Should().Be("changed Ward", "the late ward went");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restored_immunity_blocks_again_and_one_applied_since_stops_blocking()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData hit = Instant("Hit", Attribute90, -10);
		ActiveEffectHandle immunity = Apply(entity, Immunity("Immunity", hit))!;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.EffectsManager.RemoveEffect(immunity);
		EntitySimulation.RestoreSnapshot(entity, snapshot);
		Apply(entity, hit);

		entity.PlayerAttributeSet.Attribute90.CurrentValue.Should().Be(90, "the restored immunity blocks again");

		entity.EffectsManager.RemoveEffect(immunity);
		EntitySnapshot unguarded = EntitySimulation.CaptureSnapshot(entity);
		Apply(entity, Immunity("Late immunity", hit));
		EntitySimulation.RestoreSnapshot(entity, unguarded);
		Apply(entity, hit);

		entity.PlayerAttributeSet.Attribute90.CurrentValue
			.Should().Be(80, "the late immunity went with its registration");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restored_effect_follows_the_attribute_it_reads_again()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle echo = Apply(entity, Echo("Echo"))!;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		entity.EffectsManager.RemoveEffect(echo);

		EntitySimulation.RestoreSnapshot(entity, snapshot);
		Apply(entity, Instant("Hit", Attribute90, -10));

		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(
			81,
			"the echo re-reads Attribute90 when it changes, which the restore subscribed it to again");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void An_ability_granted_since_the_snapshot_goes_and_one_removed_since_comes_back()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle kept = Grant(entity, new AbilityData("Kept"));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.Abilities.ClearAbility(kept);
		AbilityHandle late = Grant(entity, new AbilityData("Late"));
		EntityReplay.Watch(entity, log);

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		kept.IsValid.Should().BeTrue();
		late.IsValid.Should().BeFalse();
		entity.Abilities.GrantedAbilities.Should().Equal(kept);
		log.Should().BeEmpty();
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void An_instance_started_since_the_snapshot_is_dropped_and_one_ended_since_runs_its_graph_again()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var graphs = new List<GraphProcessor>();
		AbilityHandle channel = Grant(entity, ChannelAbility(graphs));
		channel.TryActivate(out _).Should().BeTrue();
		entity.Abilities.FixedUpdateAbilities(0.25);

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		string captured = EntityState.Describe(entity, graphs);
		float cooldown = channel.GetRemainingCooldownTime(Tag.RequestTag(_tagsManager, "color.red"));

		channel.Cancel();
		entity.EffectsManager.UpdateEffects(1);
		channel.TryActivate(out _).Should().BeTrue();
		EntityReplay.Watch(entity, log);

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		EntityState.Describe(entity, graphs).Should().Be(captured);
		graphs[1].GraphContext.IsActive.Should().BeFalse("the instance started since was dropped, graph and all");
		channel.GetRemainingCooldownTime(Tag.RequestTag(_tagsManager, "color.red")).Should().Be(cooldown);
		log.Should().BeEmpty("nothing ended and nothing started");

		entity.Abilities.FixedUpdateAbilities(0.25);

		channel.IsActive.Should().BeFalse("the restored timer had a quarter of a second left");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restored_ability_trigger_listens_again_and_one_granted_since_stops()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle watcher = Grant(entity, Triggered("Watcher", "color.red"));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.Abilities.ClearAbility(watcher);
		AbilityHandle late = Grant(entity, Triggered("Late watcher", "color.red"));
		EntitySimulation.RestoreSnapshot(entity, snapshot);
		EntityReplay.Watch(entity, log);

		Apply(entity, Tagged("Red", "color.red"));

		watcher.IsActive.Should().BeTrue("the restored trigger watches the tags again");
		late.IsValid.Should().BeFalse();
		log.Count(x => x == "activated").Should().Be(1, "the late watcher went with its trigger");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void Handlers_sharing_an_event_run_in_the_order_they_first_subscribed_after_a_restore()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EntityReplay.Watch(entity, log);

		// Each takes hold of the entity's tags in turn: an effect, a graph's listener, then another effect.
		ActiveEffectHandle first = Apply(entity, Ward("First"))!;
		Grant(entity, ListenerAbility(log)).TryActivate(out _).Should().BeTrue();
		Apply(entity, Ward("Last"));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.EffectsManager.RemoveEffect(first);
		EntitySimulation.RestoreSnapshot(entity, snapshot);
		log.Clear();

		Apply(entity, Tagged("Red", "color.red"));

		log.Where(x => !x.StartsWith("tags", StringComparison.Ordinal)).Should().Equal(
			"changed First",
			"heard",
			"changed Last",
			"added Red");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_component_takes_hold_again_after_what_an_earlier_component_granted()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EntityReplay.Watch(entity, log);

		// The granted watcher takes hold of the tags before the ward that comes after the grant in the same effect.
		Apply(entity, GrantingWard("Granter", Triggered("Watcher", "color.red")));
		EntitySimulation.RestoreSnapshot(entity, EntitySimulation.CaptureSnapshot(entity));
		log.Clear();

		Apply(entity, Tagged("Red", "color.red"));

		log.Where(x => !x.StartsWith("tags", StringComparison.Ordinal)).Should().Equal(
			"activated",
			"ended Watcher",
			"changed Granter",
			"added Red");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_component_that_fails_to_come_back_stops_neither_the_restore_nor_what_takes_hold_again()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		Apply(
			entity,
			new EffectData(
				"Faulty",
				new DurationData(DurationType.Infinite),
				effectComponents: [new FaultyRestoreComponent()]));
		ActiveEffectHandle ward = Apply(entity, Ward("Ward"))!;
		AbilityHandle watcher = Grant(entity, Triggered("Watcher", "color.red"));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.EffectsManager.RemoveEffect(ward);
		Apply(entity, Lasting("Late", DurationType.Infinite, Flat(Attribute1, 10)));

		FluentActions.Invoking(() => EntitySimulation.RestoreSnapshot(entity, snapshot))
			.Should().Throw<InvalidOperationException>().WithMessage(FaultyRestoreComponent.Message);

		ward.IsValid.Should().BeTrue("the effects after the faulty one came back");
		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1, "the late effect went");

		Apply(entity, Tagged("Red", "color.red"));

		ward.IsInhibited.Should().BeTrue("the ward took hold of the tags again");
		watcher.IsActive.Should().BeTrue("the trigger took hold again");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restore_brings_back_shared_variables_input_random_and_body()
	{
		var input = new EntityInput(new InputActionSet(["jump"]));
		var entity = new TestEntity(_tagsManager, _cuesManager)
		{
			Input = input,
			Random = new DeterministicRandom(11),
			Mover = new StillMover(),
		};

		entity.SharedVariables.DefineVariable("charge", 1);
		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		entity.Random.NextInt();

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		int nextRoll = entity.Random.NextInt();

		entity.SharedVariables.SetVar("charge", 5);
		input.Submit(new InputFrame { Released = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);
		entity.Mover.RestoreState(new BodyState(Vector3.One, Quaternion.Identity, Vector3.UnitX, Grounded: false));

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		entity.SharedVariables.TryGetVar("charge", out int charge).Should().BeTrue();
		charge.Should().Be(1);
		input.Current.Should().Be(new InputFrame { Held = 0b1, Pressed = 0b1 });
		entity.Random.NextInt().Should().Be(nextRoll);
		entity.Mover.CaptureState().Should().Be(new StillMover().CaptureState());
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restore_brings_back_state_kept_outside_Forge_through_the_hook()
	{
		var hook = new CounterHook { Count = 3 };
		var entity = new TestEntity(_tagsManager, _cuesManager) { StateHook = hook };

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		hook.Count = 9;
		EntitySimulation.RestoreSnapshot(entity, snapshot);

		hook.Count.Should().Be(3);
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_snapshot_restores_any_number_of_times()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var graphs = new List<GraphProcessor>();
		AbilityHandle channel = Grant(entity, ChannelAbility(graphs));
		channel.TryActivate(out _).Should().BeTrue();
		Apply(entity, Stacking("Poison"));

		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		string captured = EntityState.Describe(entity, graphs);

		for (int i = 0; i < 3; i++)
		{
			EntitySimulation.Tick(entity, i + 1, 0.3);
			EntitySimulation.RestoreSnapshot(entity, snapshot);
			EntityState.Describe(entity, graphs).Should().Be(captured);
		}
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_restore_fires_no_cue()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cue, handler);

		var entity = new TestEntity(_tagsManager, cuesManager);
		var cueData = new CueData(new TagContainer(_tagsManager, [cue]), 0, 10, CueMagnitudeType.EffectLevel);
		ActiveEffectHandle kept = Apply(entity, WithCue("Kept", cueData))!;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		entity.EffectsManager.RemoveEffect(kept);
		Apply(entity, WithCue("Late", cueData));
		int applies = handler.ApplyCount;
		int removes = handler.RemoveCount;

		EntitySimulation.RestoreSnapshot(entity, snapshot);

		handler.ApplyCount.Should().Be(applies);
		handler.RemoveCount.Should().Be(removes);
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_snapshot_from_another_entity_is_refused()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var other = new TestEntity(_tagsManager, _cuesManager);

		FluentActions.Invoking(() => EntitySimulation.RestoreSnapshot(entity, EntitySimulation.CaptureSnapshot(other)))
			.Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void Snapshots_are_refused_from_inside_the_entity()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.EffectsManager.OnEffectApplied += _ => EntitySimulation.CaptureSnapshot(entity);

		FluentActions.Invoking(() => Apply(entity, Instant("Hit", Attribute1, 1)))
			.Should().Throw<InvalidOperationException>("the application is still under way");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void Snapshots_are_refused_while_an_effect_is_removed_or_updated()
	{
		var removing = new TestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle buff = Apply(removing, Lasting("Buff", DurationType.Infinite, Flat(Attribute1, 10)))!;
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(removing);
		removing.EffectsManager.OnActiveEffectRemoved += (_, _) => EntitySimulation.RestoreSnapshot(removing, snapshot);

		FluentActions.Invoking(() => removing.EffectsManager.RemoveEffect(buff))
			.Should().Throw<InvalidOperationException>("the removal is still under way");

		var updating = new TestEntity(_tagsManager, _cuesManager);
		Apply(
			updating,
			new EffectData(
				"Pulse",
				new DurationData(DurationType.Infinite),
				[Flat(Attribute1, 1)],
				periodicData: new PeriodicData(new ScalableFloat(0.25f), false, PeriodInhibitionRemovedPolicy.NeverReset)));
		updating.EffectsManager.OnEffectExecuted += _ => EntitySimulation.CaptureSnapshot(updating);

		FluentActions.Invoking(() => updating.EffectsManager.UpdateEffects(0.25))
			.Should().Throw<InvalidOperationException>("the update is still under way");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void Snapshots_are_refused_while_an_ability_activates_ends_or_goes()
	{
		var activating = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle first = Grant(activating, Triggered("First", "color.red"));
		activating.Abilities.OnAbilityActivated += _ => EntitySimulation.CaptureSnapshot(activating);

		FluentActions.Invoking(() => first.TryActivate(out _))
			.Should().Throw<InvalidOperationException>("the instance hasn't started yet");

		var ending = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle second = Grant(ending, Triggered("Second", "color.red"));
		second.TryActivate(out _).Should().BeTrue();
		ending.Abilities.OnAbilityEnded += _ => EntitySimulation.CaptureSnapshot(ending);

		FluentActions.Invoking(second.Cancel).Should().Throw<InvalidOperationException>("the instance is still ending");

		var removing = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle third = Grant(removing, Triggered("Third", "color.red"));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(removing);
		removing.Abilities.OnAbilityRemoved += _ => EntitySimulation.RestoreSnapshot(removing, snapshot);

		FluentActions.Invoking(() => removing.Abilities.ClearAbility(third))
			.Should().Throw<InvalidOperationException>("the ability is still going");
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void A_mover_that_fails_to_come_back_neither_skips_the_state_hook_nor_hides_an_earlier_failure()
	{
		var hook = new CounterHook { Count = 3 };
		var entity = new TestEntity(_tagsManager, _cuesManager) { Mover = new FaultyMover(), StateHook = hook };
		Apply(
			entity,
			new EffectData(
				"Faulty",
				new DurationData(DurationType.Infinite),
				effectComponents: [new FaultyRestoreComponent()]));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
		hook.Count = 9;

		FluentActions.Invoking(() => EntitySimulation.RestoreSnapshot(entity, snapshot))
			.Should().Throw<InvalidOperationException>().WithMessage(FaultyRestoreComponent.Message);

		hook.Count.Should().Be(3);
	}

	[Fact]
	[Trait("Simulation", "Snapshot")]
	public void Snapshots_are_refused_while_an_ability_runs_a_behavior_that_is_not_a_graph()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = Grant(entity, new AbilityData("Coded", behaviorFactory: () => new CodedBehavior()));
		EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);

		ability.TryActivate(out _).Should().BeTrue();

		FluentActions.Invoking(() => EntitySimulation.CaptureSnapshot(entity)).Should().Throw<NotSupportedException>();
		FluentActions.Invoking(() => EntitySimulation.RestoreSnapshot(entity, snapshot))
			.Should().Throw<NotSupportedException>();
	}

	private static string CarryOn(TestEntity entity, Effect effect)
	{
		EntitySnapshot start = EntitySimulation.CaptureSnapshot(entity);
		entity.EffectsManager.ApplyEffect(effect);
		entity.EffectsManager.UpdateEffects(0.5);
		string result = EntityState.Describe(entity);
		EntitySimulation.RestoreSnapshot(entity, start);
		return result;
	}

	private static AbilityHandle Grant(TestEntity entity, AbilityData abilityData)
	{
		return entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, entity);
	}

	private static ActiveEffectHandle? Apply(TestEntity entity, EffectData effectData)
	{
		return entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}

	private static DurationData Seconds(float seconds)
	{
		return new DurationData(
			DurationType.HasDuration,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(seconds)));
	}

	private static Modifier Flat(string attribute, float value)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(value)));
	}

	private static Modifier Percent(string attribute, float value)
	{
		return new Modifier(
			attribute,
			ModifierOperation.PercentBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(value)));
	}

	private static Modifier Override(string attribute, float value, int channel)
	{
		return new Modifier(
			attribute,
			ModifierOperation.Override,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(value)),
			channel);
	}

	private static EffectData Instant(string name, string attribute, float value)
	{
		return new EffectData(name, new DurationData(DurationType.Instant), [Flat(attribute, value)]);
	}

	private static EffectData Lasting(string name, DurationType durationType, params Modifier[] modifiers)
	{
		return new EffectData(name, new DurationData(durationType), modifiers);
	}

	private static EffectData Lasting(string name, DurationData duration, params Modifier[] modifiers)
	{
		return new EffectData(name, duration, modifiers);
	}

	// Adds Attribute90 to Attribute1, reading it live.
	private static EffectData Echo(string name)
	{
		var echoedFrom = new AttributeBasedFloat(
			new AttributeCaptureDefinition(Attribute90, AttributeCaptureSource.Target, Snapshot: false),
			AttributeCalculationType.CurrentValue,
			new ScalableFloat(1),
			new ScalableFloat(0),
			new ScalableFloat(0));

		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					Attribute1,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.AttributeBased, attributeBasedFloat: echoedFrom)),
			]);
	}

	private static EffectData Immunity(string name, EffectData blocked)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents: [new ImmunityEffectComponent([new EffectQuery(EffectDefinition: blocked)])]);
	}

	private static EffectData WithCue(string name, CueData cues)
	{
		return new EffectData(name, new DurationData(DurationType.Infinite), cues: [cues]);
	}

	private EffectData Stacking(string name)
	{
		var magnitude = Tag.RequestTag(_tagsManager, "simple.tag");

		return new EffectData(
			name,
			Seconds(1),
			[
				new Modifier(
					Attribute90,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(magnitude, Snapshot: false))),
			],
			new StackingData(
				new ScalableInt(5),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset),
			snapshotLevel: false);
	}

	private EffectData Tagged(string name, string tags)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(Tags(tags))]);
	}

	// Held back while the entity has the red tag.
	private EffectData Ward(string name)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: Tags("color.red"))),
			]);
	}

	// Grants the ability, then is held back while the entity has the red tag.
	private EffectData GrantingWard(string name, AbilityData abilityData)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new GrantAbilityEffectComponent(
				[
					new GrantAbilityConfig(
						abilityData,
						new ScalableInt(1),
						AbilityDeactivationPolicy.CancelImmediately,
						AbilityDeactivationPolicy.CancelImmediately),
				]),
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: Tags("color.red"))),
			]);
	}

	// Runs a half second timer under a red cooldown it commits as it starts.
	private AbilityData ChannelAbility(List<GraphProcessor> graphs)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 0.5);

		var commit = new TryCommitAbilityNode();
		graph.AddNode(commit);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			commit.InputPorts[ConditionNode.InputPort]));

		TimerNode timer = NodeBindings.CreateTimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			commit.OutputPorts[ConditionNode.TruePort],
			timer.InputPorts[ActionNode.InputPort]));

		return new AbilityData(
			"Channel",
			cooldownEffects:
			[
				new EffectData(
					"ChannelCooldown",
					Seconds(1),
					effectComponents: [new ModifierTagsEffectComponent(Tags("color.red"))]),
			],
			behaviorFactory: () =>
			{
				var behavior = new GraphAbilityBehavior(graph);
				graphs.Add(behavior.Processor);
				return behavior;
			});
	}

	// Listens for the red tag and says so.
	private AbilityData ListenerAbility(List<string> log)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty("owner", new AbilityOwnerResolver());
		graph.VariableDefinitions.DefineObjectVariable("red", Tag.RequestTag(_tagsManager, "color.red"));

		var listener = new TagListenerNode();
		listener.BindInput(TagListenerNode.EntityInput, "owner");
		listener.BindInput(TagListenerNode.TagInput, "red");
		graph.AddNode(listener);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[ActionNode.InputPort]));

		var heard = new TrackingActionNode("heard", log);
		graph.AddNode(heard);
		graph.AddConnection(new Connection(
			listener.OutputPorts[TagListenerNode.OnTagAddedPort],
			heard.InputPorts[ActionNode.InputPort]));

		return new AbilityData("Listener", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	private AbilityData Triggered(string name, string tag)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);
		AddTimer(graph);

		return new AbilityData(
			name,
			abilityTriggerData: AbilityTriggerData.ForTagAdded(Tag.RequestTag(_tagsManager, tag)),
			behaviorFactory: () => new GraphAbilityBehavior(graph));

		static void AddTimer(Graph graph)
		{
			TimerNode timer = NodeBindings.CreateTimerNode("duration");
			graph.AddNode(timer);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				timer.InputPorts[ActionNode.InputPort]));
		}
	}

	private TagContainer Tags(string tag)
	{
		return new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, tag)]);
	}

	private sealed class StillMover : IEntityMover
	{
		private BodyState _body = new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

		public void Move(double fixedDeltaTime)
		{
		}

		public BodyState CaptureState()
		{
			return _body;
		}

		public void RestoreState(in BodyState state)
		{
			_body = state;
		}
	}

	private sealed class FaultyMover : IEntityMover
	{
		public void Move(double fixedDeltaTime)
		{
		}

		public BodyState CaptureState()
		{
			return default;
		}

		public void RestoreState(in BodyState state)
		{
			throw new InvalidOperationException("The body can't be put back.");
		}
	}

	private sealed class CodedBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}

	private sealed class CounterHook : IEntityStateHook
	{
		public int Count { get; set; }

		public Variant128 CaptureState()
		{
			return new Variant128(Count);
		}

		public void RestoreState(in Variant128 state)
		{
			Count = state.AsInt();
		}
	}

	private sealed class FaultyRestoreComponent : IEffectComponent
	{
		public const string Message = "The component can't be put back.";

		public void RestoreSnapshot(object? snapshot)
		{
			throw new InvalidOperationException(Message);
		}
	}
}
