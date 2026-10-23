// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers the presentation pass: what presentation nodes show follows what is running at the end of the frame, and
/// nothing that came and went in between is shown.
/// </summary>
public class PresentationPassTests
{
	[Fact]
	[Trait("Graph", "Presentation")]
	public void Nodes_are_simulation_unless_their_type_says_otherwise()
	{
		new SimulationNode([]).NetworkKind.Should().Be(NodeNetworkKind.Simulation);
		new PresentingNode("a", []).NetworkKind.Should().Be(NodeNetworkKind.Presentation);
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void A_running_activation_is_presented_once_at_the_pass()
	{
		var log = new List<string>();
		var node = new PresentingNode("a", log);
		GraphProcessor processor = Start([node], node);

		log.Should().BeEmpty("nothing is shown before the pass");

		processor.Present();
		processor.Present();

		log.Should().Equal("start a#1");
		processor.IsPresenting.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void An_activation_that_ends_before_the_pass_is_never_presented()
	{
		var log = new List<string>();
		var node = new PresentingNode("a", log);
		GraphProcessor processor = Start([node], node);

		Abort(node, processor);
		processor.Present();

		log.Should().BeEmpty();
		processor.IsPresenting.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void An_ended_activation_ends_its_presentation_at_the_next_pass_with_what_it_returned()
	{
		var log = new List<string>();
		var node = new PresentingNode("a", log);
		GraphProcessor processor = Start([node], node);
		processor.Present();

		Abort(node, processor);
		log.Should().Equal(["start a#1"], "the presentation ends at the pass, not when the node does");

		processor.Present();

		log.Should().Equal("start a#1", "end a#1");
		processor.IsPresenting.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void A_node_restarted_several_times_between_passes_is_presented_again_once()
	{
		var log = new List<string>();
		var node = new PresentingNode("a", log, restartOnRetrigger: true);
		GraphProcessor processor = Start([node], node);
		processor.Present();

		Trigger(node, processor);
		Trigger(node, processor);
		Trigger(node, processor);
		processor.Present();

		log.Should().Equal("start a#1", "end a#1", "start a#2");
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void A_node_ended_and_started_again_between_passes_presents_the_new_activation()
	{
		var log = new List<string>();
		var keepAlive = new PresentingNode("keep", []);
		var node = new PresentingNode("a", log);
		GraphProcessor processor = Start([keepAlive, node], keepAlive, node);
		processor.Present();

		Abort(node, processor);
		Trigger(node, processor);
		processor.Present();

		log.Should().Equal("start a#1", "end a#1", "start a#2");
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void Stopping_the_graph_ends_what_it_presented_at_the_next_pass()
	{
		var log = new List<string>();
		var node = new PresentingNode("a", log);
		GraphProcessor processor = Start([node], node);
		processor.Present();

		processor.StopGraph();
		processor.IsPresenting.Should().BeTrue("no pass has run since the stop");

		processor.Present();

		log.Should().Equal("start a#1", "end a#1");
		processor.IsPresenting.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void The_pass_ends_presentations_before_it_starts_new_ones_each_in_node_order()
	{
		var log = new List<string>();
		var a = new PresentingNode("a", log);
		var b = new PresentingNode("b", log);
		var c = new PresentingNode("c", log);
		var d = new PresentingNode("d", log);
		GraphProcessor processor = Start([a, b, c, d], d, c);
		processor.Present();
		log.Should().Equal("start c#1", "start d#1");
		log.Clear();

		Trigger(b, processor);
		Trigger(a, processor);
		Abort(d, processor);
		Abort(c, processor);
		processor.Present();

		log.Should().Equal("end c#1", "end d#1", "start a#1", "start b#1");
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void Simulation_nodes_are_left_out_of_the_pass()
	{
		var log = new List<string>();
		var node = new SimulationNode(log);
		GraphProcessor processor = Start([node], node);

		processor.Present();

		log.Should().BeEmpty();
		processor.IsPresenting.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "Presentation")]
	public void A_presentation_that_throws_keeps_the_others_running_and_is_still_ended_later()
	{
		var log = new List<string>();
		var failing = new PresentingNode("a", log, throwOnStart: true);
		var other = new PresentingNode("b", log);
		GraphProcessor processor = Start([failing, other], failing, other);

		processor.Invoking(x => x.Present()).Should().Throw<InvalidOperationException>();
		log.Should().Equal("start b#1");

		Abort(failing, processor);
		processor.Present();

		log.Should().Equal("start b#1", "end nothing");
	}

	private static GraphProcessor Start(Node[] nodes, params Node[] startedByEntry)
	{
		var graph = new Graph();

		foreach (Node node in nodes)
		{
			graph.AddNode(node);
		}

		OutputPort entry = graph.EntryNode.OutputPorts[EntryNode.OutputPort];

		foreach (Node node in startedByEntry)
		{
			graph.AddConnection(new Connection(entry, node.InputPorts[StateNode<StateNodeContext>.InputPort]));
		}

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		return processor;
	}

	private static void Trigger(Node node, GraphProcessor processor)
	{
		node.InputPorts[StateNode<StateNodeContext>.InputPort].ReceiveMessage(processor.GraphContext);
	}

	private static void Abort(Node node, GraphProcessor processor)
	{
		node.InputPorts[StateNode<StateNodeContext>.AbortPort].ReceiveMessage(processor.GraphContext);
	}

	private sealed class PresentingNode(
		string name,
		List<string> log,
		bool restartOnRetrigger = false,
		bool throwOnStart = false) : StateNode<StateNodeContext>(restartOnRetrigger)
	{
		private readonly string _name = name;

		private readonly List<string> _log = log;

		private readonly bool _throwOnStart = throwOnStart;

		private int _starts;

		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnRestart(GraphContext graphContext)
		{
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			if (_throwOnStart)
			{
				throw new InvalidOperationException("The presentation failed.");
			}

			string presentation = $"{_name}#{++_starts}";
			_log.Add($"start {presentation}");
			return presentation;
		}

		protected override void OnPresentationDeactivated(object? presentation)
		{
			_log.Add($"end {presentation ?? "nothing"}");
		}
	}

	private sealed class SimulationNode(List<string> log) : StateNode<StateNodeContext>
	{
		private readonly List<string> _log = log;

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			_log.Add("start");
			return null;
		}
	}
}
