// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class FlowNodesTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Graph", "Switch")]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	[InlineData(7, 3)]
	[InlineData(-1, 3)]
	public void Switch_node_routes_by_selector(int selector, int expectedPort)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("selector", selector);

		var switchNode = new SwitchNode(caseCount: 3);
		switchNode.BindInput(SwitchNode.SelectorInput, "selector");
		graph.AddNode(switchNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			switchNode.InputPorts[SwitchNode.InputPort]));

		var trackers = new TrackingActionNode[4];

		for (int i = 0; i < trackers.Length; i++)
		{
			trackers[i] = new TrackingActionNode();
			graph.AddNode(trackers[i]);
			graph.AddConnection(new Connection(
				switchNode.OutputPorts[i],
				trackers[i].InputPorts[ActionNode.InputPort]));
		}

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		for (int i = 0; i < trackers.Length; i++)
		{
			trackers[i].ExecutionCount.Should().Be(i == expectedPort ? 1 : 0);
		}
	}

	[Fact]
	[Trait("Graph", "StateMachine")]
	public void State_machine_node_keeps_one_state_subgraph_active()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("state", 0);
		graph.VariableDefinitions.DefineVariable("currentState", -1);
		graph.VariableDefinitions.DefineVariable("childDuration", 100.0);

		var stateMachine = new StateMachineNode(stateCount: 2);
		stateMachine.BindInput(StateMachineNode.StateInput, "state");
		stateMachine.BindOutput(StateMachineNode.CurrentStateOutput, "currentState");

		TimerNode stateZeroChild = CreateTimerNode("childDuration");
		TimerNode stateOneChild = CreateTimerNode("childDuration");
		var stateZeroDeactivated = new TrackingActionNode();
		var stateChanged = new TrackingActionNode();

		graph.AddNode(stateMachine);
		graph.AddNode(stateZeroChild);
		graph.AddNode(stateOneChild);
		graph.AddNode(stateZeroDeactivated);
		graph.AddNode(stateChanged);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			stateMachine.InputPorts[StateNode<StateMachineNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			stateMachine.OutputPorts[StateMachineNode.OnStateChangedPort],
			stateChanged.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			stateMachine.OutputPorts[StateMachineNode.FirstStatePort],
			stateZeroChild.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			stateMachine.OutputPorts[StateMachineNode.FirstStatePort + 1],
			stateOneChild.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			stateZeroChild.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			stateZeroDeactivated.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		stateChanged.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetVar("currentState", out int currentState).Should().BeTrue();
		currentState.Should().Be(0);
		stateZeroDeactivated.ExecutionCount.Should().Be(0);

		// Switch to state 1: state 0's subgraph is disabled.
		processor.GraphContext.GraphVariables.SetVar("state", 1);
		processor.UpdateGraph(0.1);

		stateChanged.ExecutionCount.Should().Be(2);
		stateZeroDeactivated.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetVar("currentState", out currentState).Should().BeTrue();
		currentState.Should().Be(1);

		// Out-of-range selectors are clamped: 99 clamps to state 1, which is already active.
		processor.GraphContext.GraphVariables.SetVar("state", 99);
		processor.UpdateGraph(0.1);

		stateChanged.ExecutionCount.Should().Be(2);
	}

	[Theory]
	[Trait("Graph", "RandomBranch")]
	[InlineData(0.3, true)]
	[InlineData(0.9, false)]
	public void Random_branch_node_routes_by_probability(double roll, bool expectTrue)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("chance", 0.5);

		var branchNode = new RandomBranchNode(new FixedRandom(nextDouble: roll));
		branchNode.BindInput(RandomBranchNode.ChanceInput, "chance");

		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		graph.AddNode(branchNode);
		graph.AddNode(onTrue);
		graph.AddNode(onFalse);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			branchNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			branchNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			branchNode.OutputPorts[ConditionNode.FalsePort],
			onFalse.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(expectTrue ? 1 : 0);
		onFalse.ExecutionCount.Should().Be(expectTrue ? 0 : 1);
	}

	[Fact]
	[Trait("Graph", "TagListener")]
	public void Tag_listener_node_emits_on_watched_tag_transitions()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var watchedTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineObjectVariable("watchedTag", watchedTag);
		graph.VariableDefinitions.DefineObjectVariable<Tag>("changedTag");

		var listener = new TagListenerNode();
		listener.BindInput(TagListenerNode.EntityInput, "entity");
		listener.BindInput(TagListenerNode.TagInput, "watchedTag");
		listener.BindOutput(TagListenerNode.TagOutput, "changedTag");

		var onAdded = new TrackingActionNode();
		var onRemoved = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onAdded);
		graph.AddNode(onRemoved);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<TagListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[TagListenerNode.OnTagAddedPort],
			onAdded.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[TagListenerNode.OnTagRemovedPort],
			onRemoved.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		var tagEffectData = new EffectData(
			"Tag Granter",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(watchedTag.GetSingleTagContainer()!)]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(
			new Effect(tagEffectData, new EffectOwnership(entity, entity)));

		onAdded.ExecutionCount.Should().Be(1);
		onRemoved.ExecutionCount.Should().Be(0);
		processor.GraphContext.GraphVariables.TryGetObject("changedTag", out object? changedTag).Should().BeTrue();
		changedTag.Should().Be(watchedTag);

		entity.EffectsManager.RemoveEffect(handle!, forceRemoval: true);

		onRemoved.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Event_listener_node_deactivates_after_the_first_event_when_configured()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);

		var listener = new EventListenerNode(deactivateOnEvent: true);
		listener.BindInput(EventListenerNode.EventTagInput, "eventTag");
		listener.BindInput(EventListenerNode.ListenOnInput, "entity");

		var onEvent = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onEvent);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<EventListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[EventListenerNode.OnEventPort],
			onEvent.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		onEvent.ExecutionCount.Should().Be(1);
		processor.GraphContext.IsActive.Should().BeFalse();

		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		onEvent.ExecutionCount.Should().Be(1);
	}
}
