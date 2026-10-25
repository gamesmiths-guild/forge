// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

/// <summary>
/// Every built-in state node survives a graph snapshot. Restored from the snapshot taken after any step of a run, its
/// graph is back in that step's state and runs the steps after it exactly as it did the first time. Most nodes sit
/// under a condition monitor whose gate the steps close and open, so restores also cross their activations, and
/// listeners have to be subscribed exactly while they run.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class NodeSnapshotTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const int Steps = 8;

	private const double Delta = 0.1;

	private const double FixedStep = 1.0 / 64.0;

	private static readonly bool[] _gate = [true, true, true, false, false, true, true, false, true];

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Timer_node_replays_from_any_snapshot()
	{
		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 0.15);

		TimerNode timer = CreateTimerNode("duration");
		ConditionMonitorNode gate = Gate(graph, timer, log);
		Track(graph, timer, TimerNode.OnTimerEndPort, "end", log);
		Track(graph, timer, TimerNode.OnDeactivatePort, "off", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i => Step(processor, i),
			() => Describe(processor, gate, timer));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Loop_timer_node_replays_from_any_snapshot()
	{
		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("interval", 0.15);
		graph.VariableDefinitions.DefineVariable("loops", 3);

		var loop = new LoopTimerNode();
		loop.BindInput(LoopTimerNode.IntervalInput, "interval");
		loop.BindInput(LoopTimerNode.LoopCountInput, "loops");
		ConditionMonitorNode gate = Gate(graph, loop, log);
		Track(graph, loop, LoopTimerNode.OnIntervalPort, "interval", log);
		Track(graph, loop, LoopTimerNode.OnFinishedPort, "finished", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i => Step(processor, i),
			() => Describe(processor, gate, loop));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Repeat_node_replays_from_any_snapshot()
	{
		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 3);
		graph.VariableDefinitions.DefineVariable("interval", 0.15);
		graph.VariableDefinitions.DefineVariable("index", -1);

		var repeat = new RepeatNode();
		repeat.BindInput(RepeatNode.CountInput, "count");
		repeat.BindInput(RepeatNode.IntervalInput, "interval");
		repeat.BindOutput(RepeatNode.IndexOutput, "index");
		ConditionMonitorNode gate = Gate(graph, repeat, log);
		Track(graph, repeat, RepeatNode.OnIterationPort, "iteration", log);
		Track(graph, repeat, RepeatNode.OnFinishedPort, "finished", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i => Step(processor, i),
			() => $"{Describe(processor, gate, repeat)} index={Read<int>(processor, "index")}");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void For_each_node_replays_from_any_snapshot()
	{
		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineArrayVariable("source", 10, 20, 30);
		graph.VariableDefinitions.DefineVariable("interval", 0.15);
		graph.VariableDefinitions.DefineVariable("element", 0);
		graph.VariableDefinitions.DefineVariable("index", -1);
		graph.VariableDefinitions.DefineVariable("last", 0);

		var forEach = new ForEachNode();
		forEach.BindInput(ForEachNode.ArrayInput, "source");
		forEach.BindInput(ForEachNode.IntervalInput, "interval");
		forEach.BindOutput(ForEachNode.ElementOutput, "element");
		forEach.BindOutput(ForEachNode.IndexOutput, "index");
		ConditionMonitorNode gate = Gate(graph, forEach, log);
		Track(graph, forEach, ForEachNode.OnIterationPort, "iteration", log);
		Track(graph, forEach, ForEachNode.OnFinishedPort, "finished", log);

		// Set Variable keeps a cache in a context of its own, which a snapshot leaves out for the node to rebuild.
		SetVariableNode copy = CreateSetVariableNode("element", "last");
		graph.AddNode(copy);
		graph.AddConnection(new Connection(
			forEach.OutputPorts[ForEachNode.OnIterationPort],
			copy.InputPorts[ActionNode.InputPort]));

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i => Step(processor, i),
			() => $"{Describe(processor, gate, forEach)} element={Read<int>(processor, "element")} " +
				$"index={Read<int>(processor, "index")} last={Read<int>(processor, "last")}");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void State_machine_node_replays_from_any_snapshot()
	{
		int[] states = [0, 0, 1, 1, 2, 0, 2, 2, 1];
		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("state", states[0]);
		graph.VariableDefinitions.DefineVariable("current", -1);
		graph.VariableDefinitions.DefineVariable("duration", 0.15);

		var stateMachine = new StateMachineNode(stateCount: 3);
		stateMachine.BindInput(StateMachineNode.StateInput, "state");
		stateMachine.BindOutput(StateMachineNode.CurrentStateOutput, "current");
		graph.AddNode(stateMachine);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			stateMachine.InputPorts[ActionNode.InputPort]));
		Track(graph, stateMachine, StateMachineNode.OnStateChangedPort, "changed", log);

		var timers = new TimerNode[3];

		for (int i = 0; i < timers.Length; i++)
		{
			timers[i] = CreateTimerNode("duration");
			graph.AddNode(timers[i]);
			graph.AddConnection(new Connection(
				stateMachine.OutputPorts[StateMachineNode.FirstStatePort + i],
				timers[i].InputPorts[ActionNode.InputPort]));
			Track(graph, timers[i], TimerNode.OnTimerEndPort, $"end {i}", log);
		}

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				processor.GraphContext.GraphVariables.SetVar("state", states[i]);
				processor.UpdateGraph(Delta);
			},
			() => $"{Describe(processor, [stateMachine, .. timers])} current={Read<int>(processor, "current")}");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Input_action_node_replays_from_any_snapshot()
	{
		bool[] held = [false, true, true, false, true, true, false, false, true];
		var entity = new TestEntity(_tagsManager, _cuesManager)
		{
			Input = new EntityInput(new InputActionSet(["jump"])),
		};

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", entity);

		var watch = new InputActionNode("jump");
		watch.BindInput(InputActionNode.InputSourceInput, "source");
		graph.AddNode(watch);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[ActionNode.InputPort]));
		Track(graph, watch, InputActionNode.OnPressedPort, "pressed", log);
		Track(graph, watch, InputActionNode.OnReleasedPort, "released", log);
		Track(graph, watch, InputActionNode.WhilePressedPort, "held", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				entity.Input.Submit(new InputFrame
				{
					Held = held[i] ? 1UL : 0,
					Pressed = held[i] && !held[i - 1] ? 1UL : 0,
					Released = !held[i] && held[i - 1] ? 1UL : 0,
				});
				EntitySimulation.Tick(entity, i, FixedStep);
				processor.FixedUpdateGraph(FixedStep);
			},
			() => Describe(processor, watch));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Tag_listener_node_replays_from_any_snapshot()
	{
		bool[] present = [false, true, true, false, true, false, false, true, true];
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var watchedTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var tagEffectData = new EffectData(
			"Tag Granter",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(watchedTag.GetSingleTagContainer()!)]);
		ActiveEffectHandle? tagEffect = null;

		void SetPresence(bool isPresent)
		{
			if (isPresent && tagEffect is null)
			{
				tagEffect =
					entity.EffectsManager.ApplyEffect(new Effect(tagEffectData, new EffectOwnership(entity, entity)));
			}
			else if (!isPresent && tagEffect is not null)
			{
				entity.EffectsManager.RemoveEffect(tagEffect, forceRemoval: true);
				tagEffect = null;
			}
		}

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineObjectVariable("watchedTag", watchedTag);
		graph.VariableDefinitions.DefineObjectVariable<Tag>("changedTag");

		var listener = new TagListenerNode();
		listener.BindInput(TagListenerNode.EntityInput, "entity");
		listener.BindInput(TagListenerNode.TagInput, "watchedTag");
		listener.BindOutput(TagListenerNode.TagOutput, "changedTag");
		ConditionMonitorNode gate = Gate(graph, listener, log);
		Track(graph, listener, TagListenerNode.OnTagAddedPort, "added", log);
		Track(graph, listener, TagListenerNode.OnTagRemovedPort, "removed", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				Step(processor, i);
				SetPresence(present[i]);
			},
			() => $"{Describe(processor, gate, listener)} changed={ReadObject<Tag>(processor, "changedTag")}",
			i => SetPresence(present[i]));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Attribute_listener_node_replays_from_any_snapshot()
	{
		int[] values = [90, 85, 85, 70, 95, 95, 80, 80, 99];
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineVariable("newValue", 0);
		graph.VariableDefinitions.DefineVariable("delta", 0);

		var listener = new AttributeListenerNode("TestAttributeSet.Attribute90");
		listener.BindInput(AttributeListenerNode.EntityInput, "entity");
		listener.BindOutput(AttributeListenerNode.NewValueOutput, "newValue");
		listener.BindOutput(AttributeListenerNode.DeltaOutput, "delta");
		ConditionMonitorNode gate = Gate(graph, listener, log);
		Track(graph, listener, AttributeListenerNode.OnChangedPort, "changed", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				Step(processor, i);
				SetAttribute(entity, values[i]);
			},
			() => $"{Describe(processor, gate, listener)} newValue={Read<int>(processor, "newValue")} " +
				$"delta={Read<int>(processor, "delta")}",
			i => SetAttribute(entity, values[i]));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Event_listener_node_replays_from_any_snapshot()
	{
		bool[] raised = [false, true, true, false, true, true, false, true, true];
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineVariable("magnitude", 0f);

		EventListenerNode listener = CreateEventListenerNode("eventTag", "entity");
		listener.BindOutput(EventListenerNode.MagnitudeOutput, "magnitude");
		ConditionMonitorNode gate = Gate(graph, listener, log);
		Track(graph, listener, EventListenerNode.OnEventPort, "event", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				Step(processor, i);

				if (raised[i])
				{
					entity.Events
						.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()!, EventMagnitude = i });
				}
			},
			() => $"{Describe(processor, gate, listener)} magnitude={Read<float>(processor, "magnitude")}");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Ability_end_listener_node_replays_from_any_snapshot()
	{
		bool[] activated = [false, true, false, true, true, false, true, true, true];
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle ability = GrantInstantAbility(entity);

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineVariable("wasCanceled", true);

		var listener = new AbilityEndListenerNode();
		listener.BindInput(AbilityEndListenerNode.EntityInput, "entity");
		listener.BindOutput(AbilityEndListenerNode.WasCanceledOutput, "wasCanceled");
		ConditionMonitorNode gate = Gate(graph, listener, log);
		Track(graph, listener, AbilityEndListenerNode.OnAbilityEndedPort, "ended", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				Step(processor, i);

				if (activated[i])
				{
					ability.TryActivate(out _).Should().BeTrue();
				}
			},
			() => $"{Describe(processor, gate, listener)} wasCanceled={Read<bool>(processor, "wasCanceled")}");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Effect_level_listener_node_replays_from_any_snapshot()
	{
		int[] levels = [1, 2, 2, 3, 1, 1, 4, 2, 2];
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateFlatEffectData("Buff", DurationType.Infinite), new EffectOwnership(owner, owner));

		var log = new List<string>();
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineVariable("newLevel", 0);

		var listener = new EffectLevelListenerNode();
		listener.BindInput(EffectLevelListenerNode.EffectInput, "effect");
		listener.BindOutput(EffectLevelListenerNode.NewLevelOutput, "newLevel");
		ConditionMonitorNode gate = Gate(graph, listener, log);
		Track(graph, listener, EffectLevelListenerNode.OnLevelChangedPort, "level", log);

		GraphProcessor processor = Start(graph);

		GraphReplay.AssertRepeats(
			processor,
			log,
			Steps,
			i =>
			{
				Step(processor, i);
				effect.SetLevel(levels[i]);
			},
			() => $"{Describe(processor, gate, listener)} newLevel={Read<int>(processor, "newLevel")}",
			i => effect.SetLevel(levels[i]));
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Effect_node_comes_back_holding_its_effect_without_applying_it_again()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(CreateFlatEffectData("Buff", DurationType.Infinite)));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		EffectNode node = CreateEffectNode("effect", "target");
		AddFromEntry(graph, node);

		GraphProcessor processor = Start(graph);
		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(11);

		AssertComesBackAfterStops(processor, node);

		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(
			1,
			"the stops removed the effect, and a restore applies nothing: the world comes back with the world");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Cue_node_comes_back_holding_its_cues_without_applying_them_again()
	{
		var cuesManager = new CuesManager();
		var handler = new RecordingCueHandler();
		var cueTag = Tag.RequestTag(_tagsManager, "test.cue1");
		cuesManager.RegisterCue(cueTag, handler);

		var target = new TestEntity(_tagsManager, cuesManager);
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cueTag", cueTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		CueNode node = CreateCueNode("cueTag", "target");
		AddFromEntry(graph, node);

		GraphProcessor processor = Start(graph);
		handler.ApplyCount.Should().Be(1);

		AssertComesBackAfterStops(processor, node);

		handler.ApplyCount.Should().Be(1, "a restore applies nothing");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Grant_ability_node_comes_back_holding_its_grant_without_granting_again()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Granted");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", owner);

		var node = new GrantAbilityNode();
		node.BindInput(GrantAbilityNode.AbilityDataInput, "abilityData");
		node.BindInput(GrantAbilityNode.EntityInput, "target");
		AddFromEntry(graph, node);

		GraphProcessor processor = Start(graph);
		owner.Abilities.TryGetAbility(abilityData, out _).Should().BeTrue();

		AssertComesBackAfterStops(processor, node);

		owner.Abilities.TryGetAbility(abilityData, out _).Should().BeFalse("a restore grants nothing");
	}

	// The node's activation changes the world, which a graph snapshot leaves alone, so instead of replaying it checks
	// the context alone: stopped, restored, stopped again and restored again, it holds the very same objects it held
	// when the snapshot was taken, which also shows the stops emptied the live copies and not the snapshot's.
	private static void AssertComesBackAfterStops(GraphProcessor processor, Node node)
	{
		string running = GraphReplay.DescribeContext(processor, node);
		GraphSnapshot snapshot = processor.CaptureSnapshot();

		for (int i = 0; i < 2; i++)
		{
			processor.StopGraph();
			GraphReplay.DescribeContext(processor, node).Should().Be("no context");

			processor.RestoreSnapshot(snapshot);
			processor.GraphContext.IsActive.Should().BeTrue();
			GraphReplay.DescribeContext(processor, node).Should().Be(running);
		}
	}

	private static GraphProcessor Start(Graph graph)
	{
		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private static void Step(GraphProcessor processor, int step)
	{
		processor.GraphContext.GraphVariables.SetVar("gate", _gate[step]);
		processor.UpdateGraph(Delta);
	}

	// Puts the node under a condition monitor that watches the gate, which the steps close and open.
	private static ConditionMonitorNode Gate(Graph graph, Node node, List<string> log)
	{
		graph.VariableDefinitions.DefineVariable("gate", _gate[0]);

		var monitor = new ConditionMonitorNode();
		monitor.BindInput(ConditionMonitorNode.ConditionInput, "gate");
		AddFromEntry(graph, monitor);
		Track(graph, monitor, ConditionMonitorNode.OnBecameTruePort, "opened", log);
		Track(graph, monitor, ConditionMonitorNode.OnBecameFalsePort, "closed", log);

		graph.AddNode(node);
		graph.AddConnection(new Connection(
			monitor.OutputPorts[ConditionMonitorNode.TrueSubgraphPort],
			node.InputPorts[ActionNode.InputPort]));

		return monitor;
	}

	private static void AddFromEntry(Graph graph, Node node)
	{
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
	}

	private static void Track(Graph graph, Node node, byte port, string name, List<string> log)
	{
		var tracker = new TrackingActionNode(name, log);
		graph.AddNode(tracker);
		graph.AddConnection(new Connection(node.OutputPorts[port], tracker.InputPorts[ActionNode.InputPort]));
	}

	private static string Describe(GraphProcessor processor, params Node[] nodes)
	{
		IEnumerable<string> contexts = nodes.Select(x => GraphReplay.DescribeContext(processor, x));
		return $"active={processor.GraphContext.IsActive} {string.Join(" | ", contexts)}";
	}

	private static string Read<T>(GraphProcessor processor, string name)
		where T : unmanaged
	{
		return processor.GraphContext.GraphVariables.TryGetVar(name, out T value) ? $"{value}" : "none";
	}

	private static string ReadObject<T>(GraphProcessor processor, string name)
	{
		return processor.GraphContext.GraphVariables.TryGetObject(name, out T? value) ? $"{value}" : "none";
	}

	private static AbilityHandle GrantInstantAbility(TestEntity owner)
	{
		var behaviorGraph = new Graph();
		var abilityData = new AbilityData("Instant", behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));
		return owner.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, sourceEntity: null);
	}

	private static EffectData CreateFlatEffectData(string name, DurationType durationType)
	{
		return new EffectData(
			name,
			new DurationData(durationType),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);
	}

	private void SetAttribute(TestEntity entity, int value)
	{
		int change = value - entity.PlayerAttributeSet.Attribute90.CurrentValue;

		if (change == 0)
		{
			return;
		}

		var effectData = new EffectData(
			"Change",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(change)))
			]);

		var source = new TestEntity(_tagsManager, _cuesManager);
		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)));
	}
}
