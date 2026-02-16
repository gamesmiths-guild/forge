// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public sealed class GraphLoopDetectionTests : IDisposable
{
	public GraphLoopDetectionTests()
	{
		Validation.Enabled = true;
	}

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("LoopDetection", "Action node")]
	public void Action_node_output_connected_to_own_input_is_rejected()
	{
		var graph = new Graph();
		var action = new TrackingActionNode();
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Action node")]
	public void Two_action_nodes_forming_a_cycle_is_rejected()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Action node")]
	public void Three_action_nodes_forming_a_cycle_is_rejected()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		var action3 = new TrackingActionNode();
		graph.AddNode(action1);
		graph.AddNode(action2);
		graph.AddNode(action3);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			action3.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action3.OutputPorts[ActionNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Action node")]
	public void Linear_action_chain_is_allowed()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		var action3 = new TrackingActionNode();
		graph.AddNode(action1);
		graph.AddNode(action2);
		graph.AddNode(action3);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				action1.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				action1.OutputPorts[ActionNode.OutputPort],
				action2.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				action2.OutputPorts[ActionNode.OutputPort],
				action3.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Condition node")]
	public void Condition_true_port_looping_back_to_condition_input_is_rejected()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		graph.AddNode(condition);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			condition.InputPorts[ConditionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Condition node")]
	public void Condition_false_port_looping_back_to_condition_input_is_rejected()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: false);
		graph.AddNode(condition);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			condition.InputPorts[ConditionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Condition node")]
	public void Condition_branching_to_separate_actions_is_allowed()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();
		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				condition.InputPorts[ConditionNode.InputPort]));
			graph.AddConnection(new Connection(
				condition.OutputPorts[ConditionNode.TruePort],
				trueAction.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				condition.OutputPorts[ConditionNode.FalsePort],
				falseAction.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Condition node")]
	public void Condition_true_through_action_looping_back_is_rejected()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var action = new TrackingActionNode();
		graph.AddNode(condition);
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "State node")]
	public void State_on_activate_looping_back_to_own_input_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		graph.AddNode(timer);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnActivatePort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "State node")]
	public void State_on_deactivate_through_action_looping_back_to_own_input_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var action = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "State node")]
	public void State_on_deactivate_to_exit_node_is_allowed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var exitNode = new ExitNode();
		graph.AddNode(timer);
		graph.AddNode(exitNode);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
				exitNode.InputPorts[ExitNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "State node")]
	public void State_on_activate_to_action_is_allowed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var action = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(action);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				timer.OutputPorts[StateNode<TimerNodeContext>.OnActivatePort],
				action.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Cross-channel (safe)")]
	public void State_on_deactivate_to_another_state_abort_is_allowed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("short", 1.0);
		graph.VariableDefinitions.DefineVariable("long", 10.0);
		var shortTimer = new TimerNode("short");
		var longTimer = new TimerNode("long");
		graph.AddNode(shortTimer);
		graph.AddNode(longTimer);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				shortTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				longTimer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				shortTimer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
				longTimer.InputPorts[StateNode<TimerNodeContext>.AbortPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Cross-channel (safe)")]
	public void State_abort_output_to_another_state_input_is_allowed()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("d1", 5.0);
		graph.VariableDefinitions.DefineVariable("d2", 5.0);
		var timer1 = new TimerNode("d1");
		var timer2 = new TimerNode("d2");
		graph.AddNode(timer1);
		graph.AddNode(timer2);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				timer1.OutputPorts[StateNode<TimerNodeContext>.OnAbortPort],
				timer2.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Abort channel")]
	public void State_on_abort_looping_back_to_own_abort_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		graph.AddNode(timer);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnAbortPort],
			timer.InputPorts[StateNode<TimerNodeContext>.AbortPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Abort channel")]
	public void Two_states_abort_cycle_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("d1", 5.0);
		graph.VariableDefinitions.DefineVariable("d2", 5.0);
		var timer1 = new TimerNode("d1");
		var timer2 = new TimerNode("d2");
		graph.AddNode(timer1);
		graph.AddNode(timer2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer2.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		// timer1 abort → fires OnAbortPort → timer2 abort input
		graph.AddConnection(new Connection(
			timer1.OutputPorts[StateNode<TimerNodeContext>.OnAbortPort],
			timer2.InputPorts[StateNode<TimerNodeContext>.AbortPort]));

		// timer2 abort → fires OnAbortPort → timer1 abort input (loop!)
		Action act = () => graph.AddConnection(new Connection(
			timer2.OutputPorts[StateNode<TimerNodeContext>.OnAbortPort],
			timer1.InputPorts[StateNode<TimerNodeContext>.AbortPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Abort channel")]
	public void Abort_on_deactivate_cycle_through_action_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var action = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		// Abort fires OnAbortPort → but also fires OnDeactivatePort (via BeforeDisable)
		// So: abort input → OnDeactivatePort → action → back to abort input
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.AbortPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Subgraph port")]
	public void State_subgraph_port_looping_back_to_own_input_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		graph.AddNode(timer);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Disable-subgraph cascade")]
	public void Disable_cascade_through_state_on_deactivate_looping_back_is_rejected()
	{
		// StateNode1 subgraph → ActionNode → StateNode2 input
		// StateNode2 on_deactivate → ActionNode2 → StateNode1 input
		// When StateNode1 is disabled (via subgraph cascade), ActionNode receives disable,
		// then ActionNode propagates disable to StateNode2. StateNode2's BeforeDisable fires
		// OnDeactivatePort.EmitMessage (regular message) → ActionNode2 → back to StateNode1 input.
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("d1", 5.0);
		graph.VariableDefinitions.DefineVariable("d2", 5.0);
		var timer1 = new TimerNode("d1");
		var timer2 = new TimerNode("d2");
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(timer1);
		graph.AddNode(timer2);
		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer1.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			timer2.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer2.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			action2.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Disable-subgraph cascade")]
	public void Disable_cascade_through_action_chain_without_state_is_allowed()
	{
		// StateNode subgraph → Action1 → Action2 (no loop back)
		// Disable cascades through actions but they don't emit regular messages on disable.
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(action1);
		graph.AddNode(action2);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				timer.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
				action1.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				action1.OutputPorts[ActionNode.OutputPort],
				action2.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Disable-subgraph cascade")]
	public void Nested_state_nodes_on_deactivate_chain_loop_is_rejected()
	{
		// Entry → Timer1 (subgraph → Timer2)
		// Timer2 OnDeactivate → Timer1 input (loop via disable cascade)
		// When Timer1 disables, its SubgraphPort emits disable to Timer2.
		// Timer2's BeforeDisable fires OnDeactivatePort.EmitMessage → back to Timer1 input.
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("d1", 5.0);
		graph.VariableDefinitions.DefineVariable("d2", 3.0);
		var timer1 = new TimerNode("d1");
		var timer2 = new TimerNode("d2");
		graph.AddNode(timer1);
		graph.AddNode(timer2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer1.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			timer2.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			timer2.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
			timer1.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Complex topologies")]
	public void Diamond_topology_without_loop_is_allowed()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		var merge = new TrackingActionNode();
		graph.AddNode(condition);
		graph.AddNode(action1);
		graph.AddNode(action2);
		graph.AddNode(merge);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				condition.InputPorts[ConditionNode.InputPort]));
			graph.AddConnection(new Connection(
				condition.OutputPorts[ConditionNode.TruePort],
				action1.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				condition.OutputPorts[ConditionNode.FalsePort],
				action2.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				action1.OutputPorts[ActionNode.OutputPort],
				merge.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				action2.OutputPorts[ActionNode.OutputPort],
				merge.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Complex topologies")]
	public void Multiple_outputs_to_same_node_without_loop_is_allowed()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(action1);
		graph.AddNode(action2);

		Action act = () =>
		{
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				action1.InputPorts[ActionNode.InputPort]));
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				action2.InputPorts[ActionNode.InputPort]));
		};

		act.Should().NotThrow();
	}

	[Fact]
	[Trait("LoopDetection", "Complex topologies")]
	public void Condition_in_loop_with_action_chain_is_rejected()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(condition);
		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.FalsePort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			condition.InputPorts[ConditionNode.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Complex topologies")]
	public void State_on_activate_through_condition_and_action_looping_back_is_rejected()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 5.0);
		var timer = new TimerNode("duration");
		var condition = new FixedConditionNode(result: true);
		var action = new TrackingActionNode();
		graph.AddNode(timer);
		graph.AddNode(condition);
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnActivatePort],
			condition.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			condition.OutputPorts[ConditionNode.TruePort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		act.Should().Throw<ValidationException>();
	}

	[Fact]
	[Trait("LoopDetection", "Connection removal")]
	public void Rejected_connection_is_removed_from_graph()
	{
		var graph = new Graph();
		var action = new TrackingActionNode();
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]));

		var loopConnection = new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]);

		try
		{
			graph.AddConnection(loopConnection);
		}
		catch (ValidationException)
		{
			// Expected.
		}

		graph.Connections.Should().NotContain(loopConnection);
	}

	[Fact]
	[Trait("LoopDetection", "Connection removal")]
	public void Rejected_connection_is_disconnected_from_port()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();
		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			action1.OutputPorts[ActionNode.OutputPort],
			action2.InputPorts[ActionNode.InputPort]));

		var loopConnection = new Connection(
			action2.OutputPorts[ActionNode.OutputPort],
			action1.InputPorts[ActionNode.InputPort]);

		try
		{
			graph.AddConnection(loopConnection);
		}
		catch (ValidationException)
		{
			// Expected.
		}

		// The graph should still work normally without the loop.
		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		action1.ExecutionCount.Should().Be(1);
		action2.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("LoopDetection", "Disabled validation")]
	public void Loop_is_not_detected_when_validation_is_disabled()
	{
		Validation.Enabled = false;

		var graph = new Graph();
		var action = new TrackingActionNode();
		graph.AddNode(action);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]));

		Action act = () => graph.AddConnection(new Connection(
			action.OutputPorts[ActionNode.OutputPort],
			action.InputPorts[ActionNode.InputPort]));

		act.Should().NotThrow();
	}
}
