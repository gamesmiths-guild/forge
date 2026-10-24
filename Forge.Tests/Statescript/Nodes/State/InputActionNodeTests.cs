// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

/// <summary>
/// Covers the Input Action node: it reads its input source entity's input once per fixed step and reports the presses
/// and releases it sees, in the order they happened.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class InputActionNodeTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Graph", "InputAction")]
	public void A_tap_shorter_than_a_tick_is_reported_as_a_press_then_a_release()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		GraphProcessor processor = WatchJump(entity, log);

		entity.Input!.Submit(new InputFrame { Pressed = 0b1, Released = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().Equal("pressed", "held", "released");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void A_button_held_when_the_node_activates_starts_the_subgraph_without_a_press()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);

		GraphProcessor processor = WatchJump(entity, log);
		processor.FixedUpdateGraph(Step);
		EntitySimulation.Tick(entity, 2, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().Equal("held");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void A_release_and_a_press_in_one_tick_of_a_held_button_are_reported_in_that_order()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		GraphProcessor processor = WatchJump(entity, log, watchSubgraph: false);

		entity.Input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1, Released = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().Equal("released", "pressed");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void The_node_reads_only_on_the_fixed_rail()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		GraphProcessor processor = WatchJump(entity, log);

		entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		processor.UpdateGraph(Step);

		log.Should().BeEmpty();

		processor.FixedUpdateGraph(Step);

		log.Should().Equal("pressed", "held");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void Releasing_the_button_ends_the_while_pressed_subgraph_before_reporting_the_release()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", entity);
		graph.VariableDefinitions.DefineVariable("duration", 100.0);

		var watch = new InputActionNode("jump");
		watch.BindInput(InputActionNode.InputSourceInput, "source");
		TimerNode channel = NodeBindings.CreateTimerNode("duration");
		var pressed = new TrackingActionNode("pressed", log);
		var released = new TrackingActionNode("released", log);
		var channelEnded = new TrackingActionNode("channel ended", log);

		graph.AddNode(watch);
		graph.AddNode(channel);
		graph.AddNode(pressed);
		graph.AddNode(released);
		graph.AddNode(channelEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[StateNode<InputActionNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnReleasedPort],
			released.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.WhilePressedPort],
			channel.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			channel.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			channelEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		processor.FixedUpdateGraph(Step);
		entity.Input.Submit(new InputFrame { Released = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().Equal("pressed", "channel ended", "released");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void Deactivate_on_pressed_ends_the_node_on_the_first_press()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		GraphProcessor processor = WatchJump(entity, log, deactivateOnPressed: true);

		entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		processor.FixedUpdateGraph(Step);
		entity.Input.Submit(new InputFrame { Released = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().Equal("pressed");
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void An_action_the_input_does_not_carry_is_never_reported()
	{
		TestEntity entity = CreateEntity();
		var log = new List<string>();
		GraphProcessor processor = WatchJump(entity, log, actionName: "fire");

		entity.Input!.Submit(new InputFrame { Held = ulong.MaxValue, Pressed = ulong.MaxValue });
		EntitySimulation.Tick(entity, 1, Step);
		processor.FixedUpdateGraph(Step);

		log.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void The_input_source_defaults_to_the_abilitys_owner()
	{
		TestEntity owner = CreateEntity();

		var graph = new Graph();
		var watch = new InputActionNode("jump");
		var pressed = new TrackingActionNode();

		graph.AddNode(watch);
		graph.AddNode(pressed);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[StateNode<InputActionNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));

		ResolverTestContextFactory.ExecuteAbilityGraph(graph, owner);
		owner.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(owner, 1, Step);

		pressed.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "InputAction")]
	public void A_derived_node_can_report_a_device_read_every_frame()
	{
		var log = new List<string>();

		var graph = new Graph();
		var watch = new PolledInputActionNode { Down = true };
		var pressed = new TrackingActionNode("pressed", log);
		var released = new TrackingActionNode("released", log);
		var held = new TrackingActionNode("held", log);

		graph.AddNode(watch);
		graph.AddNode(pressed);
		graph.AddNode(released);
		graph.AddNode(held);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[StateNode<InputActionNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnReleasedPort],
			released.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.WhilePressedPort],
			held.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		watch.Down = false;
		processor.UpdateGraph(Step);
		watch.Down = true;
		processor.UpdateGraph(Step);

		log.Should().Equal("held", "released", "pressed", "held");
	}

	private static GraphProcessor WatchJump(
		TestEntity entity,
		List<string> log,
		bool watchSubgraph = true,
		bool deactivateOnPressed = false,
		string actionName = "jump")
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", entity);

		var watch = new InputActionNode(actionName, deactivateOnPressed);
		watch.BindInput(InputActionNode.InputSourceInput, "source");
		var pressed = new TrackingActionNode("pressed", log);
		var released = new TrackingActionNode("released", log);

		graph.AddNode(watch);
		graph.AddNode(pressed);
		graph.AddNode(released);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[StateNode<InputActionNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[InputActionNode.OnReleasedPort],
			released.InputPorts[ActionNode.InputPort]));

		if (watchSubgraph)
		{
			var held = new TrackingActionNode("held", log);
			graph.AddNode(held);
			graph.AddConnection(new Connection(
				watch.OutputPorts[InputActionNode.WhilePressedPort],
				held.InputPorts[ActionNode.InputPort]));
		}

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager)
		{
			Input = new EntityInput(new InputActionSet(["jump", "dash"])),
		};
	}

	// Reads a button that is not part of any entity's input, every frame, the way an engine integration reads its
	// device.
	private sealed class PolledInputActionNode() : InputActionNode("jump")
	{
		public bool Down { get; set; }

		protected override void OnActivate(GraphContext graphContext)
		{
			base.OnActivate(graphContext);
			ReadButtonState(graphContext, Down);
		}

		protected override void OnUpdate(double deltaTime, GraphContext graphContext)
		{
			ReadButtonState(graphContext, Down);
		}
	}
}
