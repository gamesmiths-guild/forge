// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers the update stamp: the monotonic counter a resolver keys on when it knows its own answer cannot change
/// within one pass.
/// </summary>
public class UpdateStampTests
{
	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void A_graph_that_has_not_been_updated_is_on_stamp_zero()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.GraphContext.UpdateStamp.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void A_frame_update_advances_the_stamp()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		processor.GraphContext.UpdateStamp.Should().Be(1);

		processor.UpdateGraph(0.016);
		processor.GraphContext.UpdateStamp.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void A_fixed_update_advances_the_stamp()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.FixedUpdateGraph(1.0 / 60.0);
		processor.GraphContext.UpdateStamp.Should().Be(1);

		processor.FixedUpdateGraph(1.0 / 60.0);
		processor.GraphContext.UpdateStamp.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void Both_rails_count_as_separate_passes()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		processor.FixedUpdateGraph(1.0 / 60.0);

		processor.GraphContext.UpdateStamp.Should().Be(
			2,
			"the frame and the fixed step are separate passes over separate world state");
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void A_node_sees_the_stamp_of_the_pass_it_is_running_in()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		node.LastSeenUpdateStamp.Should().Be(1);

		processor.FixedUpdateGraph(1.0 / 60.0);
		node.LastSeenUpdateStamp.Should().Be(2);
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void The_stamp_does_not_move_before_the_graph_starts()
	{
		var node = new TrackingStateNode();

		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);

		processor.UpdateGraph(0.016);
		processor.FixedUpdateGraph(0.016);

		processor.GraphContext.UpdateStamp.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "UpdateStamp")]
	public void The_stamp_does_not_move_after_the_graph_stops()
	{
		var node = new TrackingStateNode();
		GraphProcessor processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		processor.StopGraph();

		processor.UpdateGraph(0.016);
		processor.FixedUpdateGraph(0.016);

		processor.GraphContext.UpdateStamp.Should().Be(1);
	}

	private static GraphProcessor StartGraphWith(TrackingStateNode node)
	{
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		return processor;
	}
}
