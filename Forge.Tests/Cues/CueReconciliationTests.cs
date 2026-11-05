// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Cues;

/// <summary>
/// After a restore, the persistent cues shown are the ones the restored state holds: those of effects and Cue nodes the
/// restore took away end, those it brought back start, and those it kept are updated when they changed. No cue
/// executes.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/>.</param>
public class CueReconciliationTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void Rolling_a_world_back_and_forth_ends_and_starts_the_cues_of_the_effects_it_changes()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		var world = new WorldHistory(16, Step);
		world.Add(entity);
		Run(world, 1, 2);
		Apply(entity, WithCue("Shield", CueMagnitudeType.EffectLevel));
		Run(world, 3, 4);

		world.Rollback(2);

		handler.RemoveCount.Should().Be(1, "the shield wasn't there yet");
		handler.LastInterrupted.Should().BeTrue();

		world.Rollback(4);

		handler.ApplyCount.Should().Be(2, "the shield is back");
		handler.LastParameters!.Value.Magnitude.Should().Be(1);
		handler.ExecuteCount.Should().Be(0);
	}

	[Theory]
	[Trait("Cues", "Reconciliation")]
	[InlineData(false)]
	[InlineData(true)]
	public void A_restore_presents_nothing_on_a_host_that_doesn_t_present(bool deferred)
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		cues.Presents = false;
		cues.DeferDispatch = deferred;
		TestEntity entity = Entity(cues);
		var world = new WorldHistory(16, Step);
		world.Add(entity);
		Run(world, 1, 2);
		Apply(entity, WithCue("Shield", CueMagnitudeType.EffectLevel));
		Run(world, 3, 4);

		world.Rollback(2);
		cues.Presents = true;
		cues.PresentCues();

		handler.ApplyCount.Should().Be(0);
		handler.RemoveCount.Should().Be(0);
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_replayed_tick_shows_the_cues_of_the_effects_it_applied_once()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		cues.DeferDispatch = true;
		TestEntity entity = Entity(cues);
		EffectData shield = WithCue("Shield", CueMagnitudeType.EffectLevel);
		var world = new WorldHistory(16, Step);
		world.Add(entity);

		void Advance(long tick)
		{
			EntitySimulation.Tick(world.Entities, tick, Step);

			if (tick == 3)
			{
				Apply(entity, shield);
			}
		}

		for (long tick = 1; tick <= 4; tick++)
		{
			Advance(tick);
			world.Record(tick);
		}

		cues.PresentCues();
		world.Rollback(2);
		cues.PresentCues();

		world.Replay(4, Advance).Should().BeEmpty();
		cues.PresentCues();

		handler.ApplyCount.Should().Be(2, "the shield came back once, as recorded");
		handler.RemoveCount.Should().Be(1, "the shield the replay applied again went without being shown");
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_restore_updates_the_cues_of_an_effect_it_keeps_when_they_changed()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		EffectData stacking = WithCue("Regeneration", CueMagnitudeType.StackCount, stacks: 3);
		Apply(entity, stacking);
		EntitySnapshot oneStack = EntitySimulation.CaptureSnapshot(entity);
		Apply(entity, stacking);
		Apply(entity, stacking);

		handler.LastParameters!.Value.Magnitude.Should().Be(3);
		int updates = handler.UpdateCount;

		EntitySimulation.RestoreSnapshot(entity, oneStack);

		handler.UpdateCount.Should().Be(updates + 1);
		handler.LastParameters!.Value.Magnitude.Should().Be(1, "the effect is back to one stack");
		handler.ApplyCount.Should().Be(1);
		handler.RemoveCount.Should().Be(0);
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_handler_that_throws_as_a_restore_ends_a_cue_keeps_no_other_handler_from_ending_it()
	{
		var cues = new CuesManager();
		var failing = new RecordingCueHandler { Removed = () => throw new InvalidOperationException("Failed.") };
		var handler = new RecordingCueHandler();
		cues.RegisterCue(Tag.RequestTag(_tagsManager, "test.cue1"), failing);
		cues.RegisterCue(Tag.RequestTag(_tagsManager, "test.cue1"), handler);
		TestEntity entity = Entity(cues);
		EntitySnapshot before = EntitySimulation.CaptureSnapshot(entity);
		Apply(entity, WithCue("Shield", CueMagnitudeType.EffectLevel));

		Action restore = () => EntitySimulation.RestoreSnapshot(entity, before);

		restore.Should().Throw<InvalidOperationException>();
		handler.RemoveCount.Should().Be(1);
		handler.IsApplied.Should().BeFalse();
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_restore_shows_a_cue_as_a_handler_updated_it_while_it_applied()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		EntitySnapshot before = EntitySimulation.CaptureSnapshot(entity);
		var effect = new Effect(
			WithCue("Shield", CueMagnitudeType.EffectLevel, snapshotLevel: false),
			new EffectOwnership(entity, entity));
		handler.Applied = () => effect.SetLevel(2);
		entity.EffectsManager.ApplyEffect(effect);
		handler.Applied = null;
		EntitySnapshot leveled = EntitySimulation.CaptureSnapshot(entity);

		EntitySimulation.RestoreSnapshot(entity, before);
		EntitySimulation.RestoreSnapshot(entity, leveled);

		handler.LastParameters!.Value.Magnitude.Should().Be(2, "the handler raised the level as the cue applied");
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_restore_leaves_a_cue_alone_when_a_change_since_sent_it_the_same_parameters()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		EffectData stacking = WithCue("Regeneration", CueMagnitudeType.EffectLevel, stacks: 3);
		var regeneration = new EffectData(
			"Regeneration",
			stacking.DurationData,
			stackingData: stacking.StackingData,
			customExecutions: [new ConstantCueParameters()],
			cues: stacking.Cues);
		Apply(entity, regeneration);
		EntitySnapshot oneStack = EntitySimulation.CaptureSnapshot(entity);
		Apply(entity, regeneration);
		int updates = handler.UpdateCount;

		EntitySimulation.RestoreSnapshot(entity, oneStack);

		updates.Should().BePositive("the second stack updated the cue");
		handler.UpdateCount
			.Should().Be(updates, "the cue is sent the same at one stack or two, custom parameters included");
		handler.ApplyCount.Should().Be(1);
		handler.RemoveCount.Should().Be(0);
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void An_entity_in_limbo_shows_no_cues_until_a_rollback_brings_it_back()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		Apply(entity, WithCue("Shield", CueMagnitudeType.EffectLevel));
		var world = new WorldHistory(16, Step);
		world.Add(entity);
		Run(world, 1, 2);

		world.Remove(entity);
		Run(world, 3, 4);

		handler.RemoveCount.Should().Be(1, "an entity that leaves the world takes its cues with it");

		world.Rollback(2);

		handler.ApplyCount.Should().Be(2, "it's back, shield and all");
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_forgotten_entity_takes_its_cues_with_it_untouched()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity entity = Entity(cues);
		Apply(entity, WithCue("Shield", CueMagnitudeType.EffectLevel));
		var world = new WorldHistory(16, Step);
		world.Add(entity);
		Run(world, 1, 2);

		world.Forget(entity);
		Run(world, 3, 4);
		world.Rollback(3);

		handler.RemoveCount.Should().Be(0, "its host object may be gone");
	}

	[Fact]
	[Trait("Cues", "Reconciliation")]
	public void A_world_rollback_ends_and_starts_the_cues_a_graph_holds_on_another_entity()
	{
		CuesManager cues = Cues(out RecordingCueHandler handler);
		TestEntity caster = Entity(cues);
		TestEntity target = Entity(cues);
		var world = new WorldHistory(16, Step);
		world.Add(caster);
		world.Add(target);
		Run(world, 1, 2);

		AbilityHandle ability = caster.Abilities.GrantAbilityPermanently(
			HoldCueAbility(target),
			1,
			LevelComparison.None,
			caster);
		ability.TryActivate(out _).Should().BeTrue();
		Run(world, 3, 4);

		world.Rollback(2);

		handler.RemoveCount.Should().Be(1, "the graph wasn't holding the cue yet");
		handler.LastTarget.Should().Be(target);

		world.Rollback(4);

		handler.ApplyCount.Should().Be(2, "the graph holds it again");
		handler.LastTarget.Should().Be(target);
	}

	private static void Run(WorldHistory world, long from, long to)
	{
		for (long tick = from; tick <= to; tick++)
		{
			EntitySimulation.Tick(world.Entities, tick, Step);
			world.Record(tick);
		}
	}

	private static void Apply(TestEntity entity, EffectData effectData)
	{
		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}

	private CuesManager Cues(out RecordingCueHandler handler)
	{
		var cues = new CuesManager();
		handler = new RecordingCueHandler();
		cues.RegisterCue(Tag.RequestTag(_tagsManager, "test.cue1"), handler);
		return cues;
	}

	private TestEntity Entity(CuesManager cues)
	{
		return new TestEntity(_tagsManager, cues);
	}

	private EffectData WithCue(string name, CueMagnitudeType magnitude, int stacks = 1, bool snapshotLevel = true)
	{
		var cue = new CueData(
			new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, "test.cue1")]), 0, 3, magnitude);
		StackingData? stacking = null;

		if (stacks > 1)
		{
			stacking = new StackingData(
				new ScalableInt(stacks),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack);
		}

		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			stackingData: stacking,
			snapshotLevel: snapshotLevel,
			cues: [cue]);
	}

	// Holds the cue on the target for as long as it runs.
	private AbilityData HoldCueAbility(IForgeEntity target)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", Tag.RequestTag(_tagsManager, "test.cue1"));
		graph.VariableDefinitions.DefineObjectVariable("target", target);

		CueNode node = NodeBindings.CreateCueNode("cueTag", "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		return new AbilityData("Hold Cue", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	// Sends the same custom cue parameter whatever the effect's state.
	private sealed class ConstantCueParameters : CustomExecution
	{
		public ConstantCueParameters()
		{
			CustomCueParameters.Add("custom.parameter", 1);
		}

		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData? effectEvaluatedData)
		{
			return [];
		}
	}
}
