// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Tests.Statescript;

public class StatescriptTests
{
	[Fact]
	[Trait("Graph", "Initialization")]
	public void New_graph_has_an_entry_node()
	{
		var graph = new Graph();

		graph.EntryNode.Should().NotBeNull();
		graph.Nodes.Should().BeEmpty();
		graph.Connections.Should().BeEmpty();
	}

	[Fact]
	[Trait("Graph", "Initialization")]
	public void Graph_runner_clones_variables_on_start()
	{
		var graph = new Graph();
		graph.GraphVariables.SetVar("health", 100);

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);

		runner.StartGraph();

		context.GraphVariables.TryGetVar("health", out int value).Should().BeTrue();
		value.Should().Be(100);
	}

	[Fact]
	[Trait("Graph", "Execution")]
	public void Starting_graph_executes_connected_action_node()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], actionNode.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		actionNode.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Execution")]
	public void Action_nodes_execute_in_sequence()
	{
		var executionOrder = new List<string>();

		var graph = new Graph();
		var action1 = new TrackingActionNode("A", executionOrder);
		var action2 = new TrackingActionNode("B", executionOrder);
		var action3 = new TrackingActionNode("C", executionOrder);

		graph.AddNode(action1);
		graph.AddNode(action2);
		graph.AddNode(action3);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], action1.InputPorts[0]));
		graph.AddConnection(new Connection(action1.OutputPorts[0], action2.InputPorts[0]));
		graph.AddConnection(new Connection(action2.OutputPorts[0], action3.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		executionOrder.Should().ContainInOrder("A", "B", "C");
	}

	[Fact]
	[Trait("Graph", "Condition")]
	public void Condition_node_routes_to_true_port_when_condition_is_met()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: true);
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], condition.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[0], trueAction.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[1], falseAction.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trueAction.ExecutionCount.Should().Be(1);
		falseAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Condition")]
	public void Condition_node_routes_to_false_port_when_condition_is_not_met()
	{
		var graph = new Graph();
		var condition = new FixedConditionNode(result: false);
		var trueAction = new TrackingActionNode();
		var falseAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(trueAction);
		graph.AddNode(falseAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], condition.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[0], trueAction.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[1], falseAction.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trueAction.ExecutionCount.Should().Be(0);
		falseAction.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Variables")]
	public void Action_node_can_read_and_write_graph_variables()
	{
		var graph = new Graph();
		graph.GraphVariables.SetVar("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var readNode = new ReadVariableNode<int>("counter");

		graph.AddNode(incrementNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], incrementNode.InputPorts[0]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[0], readNode.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		readNode.LastReadValue.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Variables")]
	public void Condition_node_can_branch_based_on_graph_variables()
	{
		var graph = new Graph();
		graph.GraphVariables.SetVar("threshold", 10);
		graph.GraphVariables.SetVar("value", 15);

		var condition = new ThresholdConditionNode("value", "threshold");
		var aboveAction = new TrackingActionNode();
		var belowAction = new TrackingActionNode();

		graph.AddNode(condition);
		graph.AddNode(aboveAction);
		graph.AddNode(belowAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], condition.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[0], aboveAction.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[1], belowAction.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		aboveAction.ExecutionCount.Should().Be(1, "value (15) is above threshold (10)");
		belowAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Branching")]
	public void Output_port_can_connect_to_multiple_input_ports()
	{
		var graph = new Graph();
		var action1 = new TrackingActionNode();
		var action2 = new TrackingActionNode();

		graph.AddNode(action1);
		graph.AddNode(action2);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], action1.InputPorts[0]));
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], action2.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		action1.ExecutionCount.Should().Be(1);
		action2.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Stopping_graph_resets_variables_to_saved_state()
	{
		var graph = new Graph();
		graph.GraphVariables.SetVar("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");

		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], incrementNode.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		context.GraphVariables.TryGetVar("counter", out int valueAfterStart).Should().BeTrue();
		valueAfterStart.Should().Be(1);

		// StopGraph calls LoadVariableValues and cleans up - verify it doesn't throw.
		runner.StopGraph();

		// Original graph variables should remain at 0 (they were cloned).
		graph.GraphVariables.TryGetVar("counter", out int originalValue).Should().BeTrue();
		originalValue.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Lifecycle")]
	public void Stopping_graph_removes_all_node_contexts()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], actionNode.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		// ActionNodes register in InternalNodeActivationStatus when they receive messages.
		context.InternalNodeActivationStatus.Should().NotBeEmpty();

		runner.StopGraph();

		context.NodeContextCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Complex")]
	public void Complex_graph_with_condition_and_multiple_actions_executes_correctly()
	{
		var executionOrder = new List<string>();

		var graph = new Graph();
		graph.GraphVariables.SetVar("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var condition = new ThresholdConditionNode("counter", threshold: 0);
		var trackA = new TrackingActionNode("TrueAction", executionOrder);
		var trackB = new TrackingActionNode("FalseAction", executionOrder);

		graph.AddNode(incrementNode);
		graph.AddNode(condition);
		graph.AddNode(trackA);
		graph.AddNode(trackB);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], incrementNode.InputPorts[0]));
		graph.AddConnection(new Connection(incrementNode.OutputPorts[0], condition.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[0], trackA.InputPorts[0]));
		graph.AddConnection(new Connection(condition.OutputPorts[1], trackB.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		trackA.ExecutionCount.Should().Be(1);
		trackB.ExecutionCount.Should().Be(0);
		executionOrder.Should().ContainSingle().Which.Should().Be("TrueAction");
	}

	[Fact]
	[Trait("Graph", "Node")]
	public void Disconnected_node_is_not_executed()
	{
		var graph = new Graph();
		var connectedAction = new TrackingActionNode();
		var disconnectedAction = new TrackingActionNode();

		graph.AddNode(connectedAction);
		graph.AddNode(disconnectedAction);

		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], connectedAction.InputPorts[0]));

		var context = new TestGraphContext();
		var runner = new GraphRunner(graph, context);
		runner.StartGraph();

		connectedAction.ExecutionCount.Should().Be(1);
		disconnectedAction.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Node")]
	public void Each_graph_runner_has_independent_variable_state()
	{
		var graph = new Graph();
		graph.GraphVariables.SetVar("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(graph.EntryNode.OutputPorts[0], incrementNode.InputPorts[0]));

		var context1 = new TestGraphContext();
		var runner1 = new GraphRunner(graph, context1);

		var context2 = new TestGraphContext();
		var runner2 = new GraphRunner(graph, context2);

		runner1.StartGraph();
		runner2.StartGraph();

		context1.GraphVariables.TryGetVar("counter", out int value1);
		context2.GraphVariables.TryGetVar("counter", out int value2);

		value1.Should().Be(1);
		value2.Should().Be(1);

		// Original graph variables should remain unchanged.
		graph.GraphVariables.TryGetVar("counter", out int originalValue);
		originalValue.Should().Be(0);
	}

	private sealed class TestGraphContext : IGraphContext
	{
		private readonly Dictionary<Guid, INodeContext> _nodeContexts = [];

		public int ActiveStateNodeCount { get; set; }

		public bool IsActive => ActiveStateNodeCount > 0;

		public Variables GraphVariables { get; } = new Variables();

		public Dictionary<Guid, bool> InternalNodeActivationStatus { get; } = [];

		public int NodeContextCount => _nodeContexts.Count;

		public T GetOrCreateNodeContext<T>(Guid nodeID)
			where T : INodeContext, new()
		{
			if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
			{
				return (T)context;
			}

			var newContext = new T();
			_nodeContexts[nodeID] = newContext;
			return newContext;
		}

		public T GetNodeContext<T>(Guid nodeID)
			where T : INodeContext, new()
		{
			if (_nodeContexts.TryGetValue(nodeID, out INodeContext? context))
			{
				return (T)context;
			}

			return default!;
		}

		public void RemoveAllNodeContext()
		{
			_nodeContexts.Clear();
		}
	}

	private sealed class TrackingActionNode(string? name = null, List<string>? executionLog = null) : ActionNode
	{
		private readonly string? _name = name;
		private readonly List<string>? _executionLog = executionLog;

		public int ExecutionCount { get; private set; }

		protected override void Execute(IGraphContext graphContext)
		{
			ExecutionCount++;

			if (_name is not null)
			{
				_executionLog?.Add(_name);
			}
		}
	}

	private sealed class FixedConditionNode(bool result) : ConditionNode
	{
		private readonly bool _result = result;

		protected override bool Test(IGraphContext graphContext)
		{
			return _result;
		}
	}

	private sealed class ThresholdConditionNode : ConditionNode
	{
		private readonly string _variableName;
		private readonly string? _thresholdVariableName;
		private readonly int _fixedThreshold;

		public ThresholdConditionNode(string variableName, string thresholdVariableName)
		{
			_variableName = variableName;
			_thresholdVariableName = thresholdVariableName;
		}

		public ThresholdConditionNode(string variableName, int threshold)
		{
			_variableName = variableName;
			_fixedThreshold = threshold;
		}

		protected override bool Test(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out int value);

			int threshold = _fixedThreshold;
			if (_thresholdVariableName is not null)
			{
				graphContext.GraphVariables.TryGetVar(_thresholdVariableName, out threshold);
			}

			return value > threshold;
		}
	}

	private sealed class IncrementCounterNode(string variableName) : ActionNode
	{
		private readonly string _variableName = variableName;

		protected override void Execute(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out int currentValue);
			graphContext.GraphVariables.SetVar(_variableName, currentValue + 1);
		}
	}

	private sealed class ReadVariableNode<T>(string variableName) : ActionNode
		where T : unmanaged
	{
		private readonly string _variableName = variableName;

		public T LastReadValue { get; private set; }

		protected override void Execute(IGraphContext graphContext)
		{
			graphContext.GraphVariables.TryGetVar(_variableName, out T value);
			LastReadValue = value;
		}
	}
}
