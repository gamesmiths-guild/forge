// Copyright © Gamesmiths Guild.

using System.Reflection;
using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class StateNodeRetriggerTests
{
	private const string RestartParameterName = "restartOnRetrigger";

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_retrigger_is_ignored_by_default()
	{
		var graph = new Graph();
		var node = new RestartableTrackingNode();
		var child = new TrackingStateNode();
		TrackingActionNode onActivate = ConnectActivationTracker(graph, node);
		AddSubgraphChild(graph, node, child);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		node.ActivateCount.Should().Be(1);
		node.RestartCount.Should().Be(0);
		node.ActivatedCount.Should().Be(1);
		onActivate.ExecutionCount.Should().Be(1);
		child.ActivateCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_node_built_to_restart_starts_over_on_a_retrigger()
	{
		var graph = new Graph();
		var node = new RestartableTrackingNode(restartOnRetrigger: true);
		TrackingActionNode onActivate = ConnectActivationTracker(graph, node);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		node.ActivateCount.Should().Be(1);
		node.RestartCount.Should().Be(1);
		node.DeactivateCount.Should().Be(0);
		node.ActivatedCount.Should().Be(2);
		onActivate.ExecutionCount.Should().Be(2);
		processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_restart_retriggers_the_subgraph_instead_of_rebuilding_it()
	{
		var graph = new Graph();
		var parent = new RestartableTrackingNode(restartOnRetrigger: true);
		var ignoringChild = new TrackingStateNode();
		var restartingChild = new RestartableTrackingNode(restartOnRetrigger: true);
		graph.AddNode(parent);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			parent.InputPorts[ActionNode.InputPort]));
		AddSubgraphChild(graph, parent, ignoringChild);
		AddSubgraphChild(graph, parent, restartingChild);
		ConnectRetrigger(graph, parent, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		parent.RestartCount.Should().Be(1);
		ignoringChild.ActivateCount.Should().Be(1);
		ignoringChild.DeactivateCount.Should().Be(0);
		restartingChild.ActivateCount.Should().Be(1);
		restartingChild.RestartCount.Should().Be(1);
		restartingChild.DeactivateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_restarted_node_is_first_updated_on_the_next_pass_wherever_it_sits_in_this_one()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 10.0);
		graph.VariableDefinitions.DefineVariable("retriggerDelay", 1.0);

		// Activated around the retrigger, so one is updated before it fires in the pass and the other after.
		TimerNode updatedBefore = CreateRestartingTimer("duration");
		TimerNode retrigger = CreateTimerNode("retriggerDelay");
		TimerNode updatedAfter = CreateRestartingTimer("duration");

		foreach (Node node in (Node[])[updatedBefore, retrigger, updatedAfter])
		{
			graph.AddNode(node);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				node.InputPorts[ActionNode.InputPort]));
		}

		graph.AddConnection(new Connection(
			retrigger.OutputPorts[TimerNode.OnTimerEndPort],
			updatedBefore.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			retrigger.OutputPorts[TimerNode.OnTimerEndPort],
			updatedAfter.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		ElapsedTime(processor, updatedBefore).Should().Be(0);
		ElapsedTime(processor, updatedAfter).Should().Be(0, "the pass's delta is time from before the retrigger");
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_node_ended_and_started_again_is_first_updated_on_the_next_pass_wherever_it_sits_in_this_one()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 10.0);
		graph.VariableDefinitions.DefineVariable("restartDelay", 1.0);

		// Activated around the timer that aborts and starts them again, so one is updated before it fires in the pass
		// and the other after.
		TimerNode updatedBefore = CreateTimerNode("duration");
		TimerNode restarter = CreateTimerNode("restartDelay");
		TimerNode updatedAfter = CreateTimerNode("duration");

		foreach (Node node in (Node[])[updatedBefore, restarter, updatedAfter])
		{
			graph.AddNode(node);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				node.InputPorts[TimerNode.InputPort]));
		}

		foreach (TimerNode timer in (TimerNode[])[updatedBefore, updatedAfter])
		{
			graph.AddConnection(new Connection(
				restarter.OutputPorts[TimerNode.OnTimerEndPort],
				timer.InputPorts[TimerNode.AbortPort]));
			graph.AddConnection(new Connection(
				restarter.OutputPorts[TimerNode.OnTimerEndPort],
				timer.InputPorts[TimerNode.InputPort]));
		}

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		ElapsedTime(processor, updatedBefore).Should().Be(0);
		ElapsedTime(processor, updatedAfter).Should().Be(0, "the pass's delta is time from before it started again");
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void An_ending_requested_by_a_restart_waits_until_the_restart_has_emitted()
	{
		var log = new List<string>();
		var graph = new Graph();
		var node = new EndOnRestartNode();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
		ConnectLogger(graph, node, EndOnRestartNode.OnActivatePort, "activate", log);
		ConnectLogger(graph, node, EndOnRestartNode.OnDeactivatePort, "deactivate", log);
		ConnectLogger(graph, node, EndOnRestartNode.OnEndedPort, "ended", log);
		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		log.Should().Equal("activate", "activate", "deactivate", "ended");
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_retrigger_reaching_a_node_while_it_starts_is_ignored()
	{
		var log = new List<string>();
		var graph = new Graph();
		var node = new RetriggersItselfOnStartNode();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
		ConnectLogger(graph, node, RetriggersItselfOnStartNode.OnActivatePort, "activate", log);
		ConnectLogger(graph, node, RetriggersItselfOnStartNode.SubgraphPort, "subgraph", log);
		ConnectLogger(graph, node, RetriggersItselfOnStartNode.OnEmittedPort, "emitted", log);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		node.RestartCount.Should().Be(0);
		log.Should().Equal("activate", "subgraph", "emitted");
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void A_node_that_has_ended_activates_again_instead_of_restarting()
	{
		var graph = new Graph();
		var node = new RestartableTrackingNode(restartOnRetrigger: true);
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		graph.VariableDefinitions.DefineVariable("abortAt", 0.5);
		TimerNode abortTimer = CreateTimerNode("abortAt");
		graph.AddNode(abortTimer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			abortTimer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			abortTimer.OutputPorts[TimerNode.OnTimerEndPort],
			node.InputPorts[RestartableTrackingNode.AbortPort]));

		ConnectRetrigger(graph, node, 1.0);

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		processor.UpdateGraph(0.5);
		processor.UpdateGraph(0.5);

		node.ActivateCount.Should().Be(2);
		node.DeactivateCount.Should().Be(1);
		node.RestartCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void Every_state_node_that_can_restart_offers_the_choice_in_its_constructor_and_no_other_does()
	{
		string[] mismatched = [.. ConcreteStateNodeTypes()
			.Where(x => OverridesRestart(x) != TakesRestartChoice(x))
			.Select(x => x.FullName!)];

		mismatched.Should().BeEmpty(
			"a node that overrides OnRestart has to let an editor ask for it through a '{0}' constructor parameter, " +
			"and one that does not would offer a choice that changes nothing",
			RestartParameterName);
	}

	[Fact]
	[Trait("Graph", "Retrigger")]
	public void Every_restart_choice_reaches_the_base_node()
	{
		string[] dropped = [.. ConcreteStateNodeTypes()
			.Where(TakesRestartChoice)
			.Where(x => !BuildsRestarting(x))
			.Select(x => x.FullName!)];

		dropped.Should().BeEmpty("the choice only works when the node passes it on to the base constructor");
	}

	private static TrackingActionNode ConnectActivationTracker(Graph graph, RestartableTrackingNode node)
	{
		var onActivate = new TrackingActionNode();
		graph.AddNode(node);
		graph.AddNode(onActivate);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			node.OutputPorts[RestartableTrackingNode.OnActivatePort],
			onActivate.InputPorts[ActionNode.InputPort]));

		return onActivate;
	}

	private static void AddSubgraphChild(Graph graph, Node parent, Node child)
	{
		graph.AddNode(child);
		graph.AddConnection(new Connection(
			parent.OutputPorts[RestartableTrackingNode.SubgraphPort],
			child.InputPorts[ActionNode.InputPort]));
	}

	private static void ConnectLogger(Graph graph, Node node, byte outputPort, string name, List<string> log)
	{
		var logger = new TrackingActionNode(name, log);
		graph.AddNode(logger);
		graph.AddConnection(new Connection(node.OutputPorts[outputPort], logger.InputPorts[ActionNode.InputPort]));
	}

	private static TimerNode CreateRestartingTimer(string durationPropertyName)
	{
		var timer = new TimerNode(restartOnRetrigger: true);
		timer.BindInput(TimerNode.DurationInput, durationPropertyName);
		return timer;
	}

	private static double ElapsedTime(GraphProcessor processor, TimerNode timer)
	{
		return processor.GraphContext.GetNodeContext<TimerNodeContext>(timer.NodeID).ElapsedTime;
	}

	private static IEnumerable<Type> ConcreteStateNodeTypes()
	{
		return typeof(StateNode<>).Assembly.GetTypes().Where(x => !x.IsAbstract && IsStateNode(x));
	}

	private static bool IsStateNode(Type type)
	{
		for (Type? current = type.BaseType; current is not null; current = current.BaseType)
		{
			if (IsBaseStateNode(current))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsBaseStateNode(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StateNode<>);
	}

	private static bool OverridesRestart(Type type)
	{
		MethodInfo onRestart = type.GetMethod("OnRestart", BindingFlags.Instance | BindingFlags.NonPublic)!;
		return !IsBaseStateNode(onRestart.DeclaringType!);
	}

	private static bool TakesRestartChoice(Type type)
	{
		return type.GetConstructors()
			.SelectMany(x => x.GetParameters())
			.Any(x => x.Name == RestartParameterName && x.ParameterType == typeof(bool));
	}

	private static bool BuildsRestarting(Type type)
	{
		ConstructorInfo constructor = type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
		object?[] arguments = [.. constructor.GetParameters().Select(ArgumentAskingForRestart)];

		object node = constructor.Invoke(arguments);
		return (bool)type.GetProperty("RestartOnRetrigger")!.GetValue(node)!;
	}

	private static object? ArgumentAskingForRestart(ParameterInfo parameter)
	{
		if (parameter.Name == RestartParameterName)
		{
			return true;
		}

		return parameter.HasDefaultValue ? parameter.DefaultValue : null;
	}

	private sealed class EndOnRestartNode() : StateNode<StateNodeContext>(restartOnRetrigger: true)
	{
		public const byte OnEndedPort = 4;

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnEndedPort, "OnEnded"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnRestart(GraphContext graphContext)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnEndedPort);
		}
	}

	// Emits from its activation, then retriggers itself from there, once, as an event its activation sets off might.
	private sealed class RetriggersItselfOnStartNode() : StateNode<StateNodeContext>(restartOnRetrigger: true)
	{
		public const byte OnEmittedPort = 4;

		private bool _retriggered;

		public int RestartCount { get; private set; }

		protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
		{
			base.DefinePorts(inputPorts, outputPorts);
			outputPorts.Add(CreatePort<EventPort>(OnEmittedPort, "OnEmitted"));
		}

		protected override void OnActivate(GraphContext graphContext)
		{
			EmitMessage(graphContext, OnEmittedPort);

			if (!_retriggered)
			{
				_retriggered = true;
				InputPorts[InputPort].ReceiveMessage(graphContext);
			}
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnRestart(GraphContext graphContext)
		{
			RestartCount++;
		}
	}
}
