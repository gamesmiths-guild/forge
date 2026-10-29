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
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// The replay harness. An entity driven by input alone is rolled back to every tick it recorded and replayed from
/// there, and has to raise the same events and end each tick in the same state as the first time: stacking, periodic
/// and expiring effects, input-bound and triggered abilities with the graphs they run, effects that grant abilities,
/// its random source and its body.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class EntityHistoryTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	// A power of two, so durations and periods authored as floats are whole numbers of steps.
	private const double Step = 1.0 / 64.0;

	private const int Ticks = 64;

	private const string Speed = "TestAttributeSet.Attribute5";

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Simulation", "History")]
	public void A_history_keeps_the_most_recent_ticks()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 3, Step);

		history.OldestTick.Should().BeNull();
		history.NewestTick.Should().BeNull();

		RunAndRecord(entity, history, 1, 5);

		history.OldestTick.Should().Be(3);
		history.NewestTick.Should().Be(5);
		history.Invoking(x => x.Rollback(2)).Should().Throw<ArgumentOutOfRangeException>();
		history.Invoking(x => x.Rollback(6)).Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_tick_recorded_again_forgets_the_ticks_after_it()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 8, Step);
		RunAndRecord(entity, history, 1, 5);

		history.Rollback(2);
		RunAndRecord(entity, history, 3, 3);

		history.OldestTick.Should().Be(1);
		history.NewestTick.Should().Be(3, "ticks 4 and 5 belonged to the run tick 3 replaced");
		history.Invoking(x => x.Rollback(4)).Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_tick_that_does_not_follow_the_newest_starts_the_history_over()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 8, Step);
		RunAndRecord(entity, history, 1, 3);

		RunAndRecord(entity, history, 10, 10);

		history.OldestTick.Should().Be(10);
		history.NewestTick.Should().Be(10);
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_tick_that_cannot_be_recorded_leaves_the_history_as_it_was()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 8, Step);
		RunAndRecord(entity, history, 1, 2);

		AbilityHandle coded = entity.Abilities.GrantAbilityPermanently(
			new AbilityData("Coded", behaviorFactory: () => new CodedBehavior()),
			1,
			LevelComparison.None,
			entity);
		coded.TryActivate(out _).Should().BeTrue();
		EntitySimulation.Tick(entity, 3, Step);

		history.Invoking(x => x.Record(3)).Should().Throw<NotSupportedException>();

		history.NewestTick.Should().Be(2);
		history.Invoking(x => x.Rollback(3)).Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_replay_runs_only_the_ticks_after_the_current_one_up_to_the_newest()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 8, Step);
		RunAndRecord(entity, history, 1, 4);

		history.Invoking(x => x.Replay(4)).Should().Throw<ArgumentOutOfRangeException>(
			"the entity is already at the newest tick");

		history.Rollback(1);

		history.Invoking(x => x.Replay(1)).Should().Throw<ArgumentOutOfRangeException>();
		history.Invoking(x => x.Replay(5)).Should().Throw<ArgumentOutOfRangeException>();

		history.Replay(3);
		history.Replay(4);
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_correction_made_after_a_rollback_is_what_the_next_rollback_brings_back()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var history = new EntityHistory(entity, capacity: 8, Step);
		RunAndRecord(entity, history, 1, 4);

		history.Rollback(2);
		int corrected = entity.Attributes[Speed].BaseValue + 3;
		entity.EffectsManager.ApplyEffect(
			new Effect(Instant("Correction", Speed, 3), new EffectOwnership(entity, entity)));
		history.Replay(4);
		history.Rollback(2);

		entity.Attributes[Speed].BaseValue.Should().Be(corrected, "the replay recorded tick 2 as corrected");
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_replay_repeats_stacking_periodic_and_expiring_effects()
	{
		var log = new List<string>();
		var graphs = new List<GraphProcessor>();
		var input = new EntityInput(new InputActionSet(["burn", "ward"]));
		var entity = new TestEntity(_tagsManager, _cuesManager) { Input = input };
		EntityReplay.Watch(entity, log);
		Grant(entity, BurnAbility(graphs));
		Grant(entity, WardAbility(graphs, withTotem: false));

		Press[] presses = [new("burn", 2), new("ward", 6), new("burn", 34), new("ward", 40)];

		EntityReplay.AssertRepeats(
			entity,
			Ticks,
			Step,
			tick => input.Submit(Frame(input, tick, presses)),
			() => EntityState.Describe(entity, graphs),
			log);
	}

	[Fact]
	[Trait("Simulation", "History")]
	public void A_replay_repeats_triggered_and_granted_abilities_their_listeners_random_and_body()
	{
		var log = new List<string>();
		var graphs = new List<GraphProcessor>();
		var input = new EntityInput(new InputActionSet(["burn", "ward", "right"]));
		var entity = new TestEntity(_tagsManager, _cuesManager) { Input = input, Random = new DeterministicRandom(7) };
		entity.Mover = new WalkingMover(entity);

		// Watched before anything is granted, the way outside code that only watches subscribes once at the start: a
		// restore takes hold again after it, so a trigger granted earlier would run after it from then on.
		EntityReplay.Watch(entity, log);
		Grant(entity, BurnAbility(graphs));
		Grant(entity, WardAbility(graphs, withTotem: true));
		Grant(entity, WatchAbility(graphs));
		Grant(entity, EchoAbility(graphs));

		Press[] presses = [new("burn", 2), new("ward", 6), new("burn", 34), new("ward", 40)];

		EntityReplay.AssertRepeats(
			entity,
			Ticks,
			Step,
			tick => input.Submit(Frame(input, tick, presses, walkFrom: 3, walkTo: 44)),
			() => EntityState.Describe(entity, graphs),
			log);
	}

	private static void RunAndRecord(IForgeEntity entity, EntityHistory history, long from, long to)
	{
		for (long tick = from; tick <= to; tick++)
		{
			EntitySimulation.Tick(entity, tick, Step);
			history.Record(tick);
		}
	}

	// Presses each action on its tick and lets go of it on the next. "right" is held through the walk.
	private static InputFrame Frame(
		EntityInput input,
		long tick,
		Press[] presses,
		long walkFrom = -1,
		long walkTo = -1)
	{
		ulong held = 0;
		ulong pressed = 0;
		ulong released = 0;

		foreach (Press press in presses)
		{
			ulong bit = 1UL << input.Actions.IndexOf(press.Action);

			if (tick == press.Tick)
			{
				held |= bit;
				pressed |= bit;
			}
			else if (tick == press.Tick + 1)
			{
				released |= bit;
			}
		}

		if (walkFrom >= 0)
		{
			ulong bit = 1UL << input.Actions.IndexOf("right");
			held |= tick >= walkFrom && tick <= walkTo ? bit : 0;
			pressed |= tick == walkFrom ? bit : 0;
			released |= tick == walkTo + 1 ? bit : 0;
		}

		return new InputFrame { Held = held, Pressed = pressed, Released = released };
	}

	private static void Grant(TestEntity entity, AbilityData abilityData)
	{
		entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, entity);
	}

	private static GraphAbilityBehavior Track(List<GraphProcessor> graphs, GraphAbilityBehavior behavior)
	{
		graphs.Add(behavior.Processor);
		return behavior;
	}

	private static void AddFromEntry(Graph graph, Node node)
	{
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
	}

	private static void Connect(Graph graph, Node from, byte port, Node to)
	{
		graph.AddNode(to);
		graph.AddConnection(new Connection(from.OutputPorts[port], to.InputPorts[ActionNode.InputPort]));
	}

	private static ApplyEffectNode ApplyToOwner(Graph graph, string effectVariable, EffectData effectData)
	{
		graph.VariableDefinitions.DefineObjectProperty(effectVariable, new EffectFromDataResolver(effectData));

		var node = new ApplyEffectNode();
		node.BindInput(ApplyEffectNode.EffectInput, effectVariable);
		node.BindInput(ApplyEffectNode.TargetInput, "owner");
		return node;
	}

	private static Graph NewAbilityGraph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty("owner", new AbilityOwnerResolver());
		return graph;
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

	private static EffectData Instant(string name, string attribute, float value)
	{
		return new EffectData(name, new DurationData(DurationType.Instant), [Flat(attribute, value)]);
	}

	// Granted by the totem, it pulses every eighth of a second until the totem takes it away.
	private static AbilityData PulseAbility(List<GraphProcessor> graphs)
	{
		Graph graph = NewAbilityGraph();
		graph.VariableDefinitions.DefineVariable("interval", 0.125);

		var loop = new LoopTimerNode();
		loop.BindInput(LoopTimerNode.IntervalInput, "interval");
		AddFromEntry(graph, loop);
		Connect(
			graph,
			loop,
			LoopTimerNode.OnIntervalPort,
			ApplyToOwner(graph, "pulseHit", Instant("PulseHit", "TestAttributeSet.Attribute1", 1)));

		return new AbilityData("Pulse", behaviorFactory: () => Track(graphs, new GraphAbilityBehavior(graph)));
	}

	private static EffectData Burning()
	{
		return new EffectData(
			"Burning",
			Seconds(0.25f),
			[Flat("TestAttributeSet.Attribute90", -1)],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private static EffectData Totem(List<GraphProcessor> graphs)
	{
		return new EffectData(
			"Totem",
			Seconds(0.25f),
			effectComponents:
			[
				new GrantAbilityEffectComponent(
				[
					new GrantAbilityConfig(
						PulseAbility(graphs),
						new ScalableInt(1),
						AbilityDeactivationPolicy.CancelImmediately,
						AbilityDeactivationPolicy.CancelImmediately,
						TryActivateOnGrant: true),
				]),
			]);
	}

	private static EffectData Scorch()
	{
		return Instant("Scorch", "TestAttributeSet.Attribute1000", 1);
	}

	// Burns three times, a sixteenth of a second apart, then ends. The burn stacks and ticks on its own.
	private AbilityData BurnAbility(List<GraphProcessor> graphs)
	{
		Graph graph = NewAbilityGraph();
		graph.VariableDefinitions.DefineVariable("interval", 0.0625);
		graph.VariableDefinitions.DefineVariable("loops", 3);

		var commit = new TryCommitAbilityNode();
		AddFromEntry(graph, commit);

		var loop = new LoopTimerNode();
		loop.BindInput(LoopTimerNode.IntervalInput, "interval");
		loop.BindInput(LoopTimerNode.LoopCountInput, "loops");
		Connect(graph, commit, ConditionNode.TruePort, loop);
		Connect(graph, loop, LoopTimerNode.OnIntervalPort, ApplyToOwner(graph, "burning", Burning()));

		return new AbilityData(
			"Burn",
			costEffect: Instant("BurnCost", "TestAttributeSet.Attribute100", -10),
			cooldownEffects: [Cooldown("BurnCooldown", 0.25f, "color.red")],
			behaviorFactory: () => Track(graphs, new GraphAbilityBehavior(graph)),
			inputBinding: AbilityInputBinding.ForAction("burn"));
	}

	// Lasts as long as its ward, which is held back while burning's cooldown shows. With a totem, it also grants an
	// ability that pulses for a while.
	private AbilityData WardAbility(List<GraphProcessor> graphs, bool withTotem)
	{
		Graph graph = NewAbilityGraph();
		graph.VariableDefinitions.DefineObjectProperty("warded", new EffectFromDataResolver(Warded()));

		var ward = new EffectNode();
		ward.BindInput(EffectNode.EffectInput, "warded");
		ward.BindInput(EffectNode.TargetInput, "owner");
		AddFromEntry(graph, ward);

		if (withTotem)
		{
			AddFromEntry(graph, ApplyToOwner(graph, "totem", Totem(graphs)));
		}

		return new AbilityData(
			"Ward",
			behaviorFactory: () => Track(graphs, new GraphAbilityBehavior(graph)),
			inputBinding: AbilityInputBinding.ForAction("ward"));
	}

	// Starts when the ward's tag arrives and keeps watch for half a second: a burn's cooldown tag scorches and echoes,
	// and every change to the burned attribute rolls for luck.
	private AbilityData WatchAbility(List<GraphProcessor> graphs)
	{
		Graph graph = NewAbilityGraph();
		graph.VariableDefinitions.DefineObjectVariable("red", Tag.RequestTag(_tagsManager, "color.red"));
		graph.VariableDefinitions.DefineObjectVariable("echo", Tag.RequestTag(_tagsManager, "simple.tag"));
		graph.VariableDefinitions.DefineVariable("chance", 0.5);
		graph.VariableDefinitions.DefineVariable("watch", 0.5);

		var tagListener = new TagListenerNode();
		tagListener.BindInput(TagListenerNode.EntityInput, "owner");
		tagListener.BindInput(TagListenerNode.TagInput, "red");
		AddFromEntry(graph, tagListener);
		Connect(graph, tagListener, TagListenerNode.OnTagAddedPort, ApplyToOwner(graph, "scorch", Scorch()));

		var raise = new RaiseEventNode();
		raise.BindInput(RaiseEventNode.EventTagInput, "echo");
		raise.BindInput(RaiseEventNode.TargetInput, "owner");
		Connect(graph, tagListener, TagListenerNode.OnTagAddedPort, raise);

		var attributeListener = new AttributeListenerNode("TestAttributeSet.Attribute90");
		attributeListener.BindInput(AttributeListenerNode.EntityInput, "owner");
		AddFromEntry(graph, attributeListener);

		var roll = new RandomBranchNode();
		roll.BindInput(RandomBranchNode.ChanceInput, "chance");
		Connect(graph, attributeListener, AttributeListenerNode.OnChangedPort, roll);
		Connect(
			graph,
			roll,
			ConditionNode.TruePort,
			ApplyToOwner(graph, "luck", Instant("Luck", "TestAttributeSet.Attribute3", 1)));

		var timer = new TimerNode();
		timer.BindInput(TimerNode.DurationInput, "watch");
		AddFromEntry(graph, timer);
		Connect(graph, timer, TimerNode.OnTimerEndPort, new ExitNode());

		return new AbilityData(
			"Watch",
			abilityTriggerData: AbilityTriggerData.ForTagAdded(Tag.RequestTag(_tagsManager, "color.blue")),
			behaviorFactory: () => Track(graphs, new GraphAbilityBehavior(graph)));
	}

	// Answers the watch's echo event at once.
	private AbilityData EchoAbility(List<GraphProcessor> graphs)
	{
		Graph graph = NewAbilityGraph();
		AddFromEntry(graph, ApplyToOwner(graph, "echoHit", Instant("EchoHit", "TestAttributeSet.Attribute2", 1)));

		return new AbilityData(
			"Echo",
			abilityTriggerData: AbilityTriggerData.ForEvent(Tag.RequestTag(_tagsManager, "simple.tag")),
			behaviorFactory: () => Track(graphs, new GraphAbilityBehavior(graph)));
	}

	private EffectData Warded()
	{
		return new EffectData(
			"Warded",
			Seconds(0.375f),
			[Flat(Speed, 5)],
			effectComponents:
			[
				new ModifierTagsEffectComponent(Tags("color.blue")),
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: Tags("color.red"))),
			]);
	}

	private EffectData Cooldown(string name, float seconds, string tag)
	{
		return new EffectData(name, Seconds(seconds), effectComponents: [new ModifierTagsEffectComponent(Tags(tag))]);
	}

	private TagContainer Tags(string tag)
	{
		return new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, tag)]);
	}

	private readonly record struct Press(string Action, long Tick);

	// Walks right while "right" is held, at the entity's speed attribute in units per second.
	private sealed class WalkingMover(TestEntity entity) : IEntityMover
	{
		private readonly TestEntity _entity = entity;

		private BodyState _body = new(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Grounded: true);

		public void Move(double fixedDeltaTime)
		{
			float speed = _entity.Input!.IsHeld("right") ? _entity.Attributes[Speed].CurrentValue : 0;
			var velocity = new Vector3(speed, 0, 0);
			_body = _body with { Position = _body.Position + (velocity * (float)fixedDeltaTime), Velocity = velocity };
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

	private sealed class CodedBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
