// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers taking a snapshot of a graph run and restoring it: what comes back, what stays, and what runs along the way.
/// </summary>
public class GraphSnapshotTests
{
	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_restore_brings_back_the_running_nodes_their_contexts_and_the_graph_variables()
	{
		(GraphProcessor processor, TimerNode timer) = StartTimer();
		processor.UpdateGraph(0.25);
		GraphSnapshot snapshot = processor.CaptureSnapshot();

		processor.GraphContext.GraphVariables.SetArrayElement("history", 0, 9);
		processor.UpdateGraph(1.0);
		processor.GraphContext.IsActive.Should().BeFalse();
		ReadCount(processor).Should().Be(1);

		processor.RestoreSnapshot(snapshot);

		processor.GraphContext.IsActive.Should().BeTrue();
		processor.GraphContext.GetNodeContext<TimerNodeContext>(timer.NodeID).ElapsedTime.Should().Be(0.25);
		ReadCount(processor).Should().Be(0);
		processor.GraphContext.GraphVariables.TryGetArrayElement("history", 0, out int first).Should().BeTrue();
		first.Should().Be(1);

		processor.UpdateGraph(0.5);
		processor.GraphContext.IsActive.Should().BeTrue();
		processor.UpdateGraph(0.25);
		processor.GraphContext.IsActive.Should().BeFalse("the restored timer finishes on its own time");
		ReadCount(processor).Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_snapshot_is_left_as_it_was_and_restores_any_number_of_times()
	{
		(GraphProcessor processor, TimerNode timer) = StartTimer();
		processor.UpdateGraph(0.25);
		GraphSnapshot snapshot = processor.CaptureSnapshot();

		for (int i = 0; i < 3; i++)
		{
			processor.RestoreSnapshot(snapshot);

			processor.GraphContext.GetNodeContext<TimerNodeContext>(timer.NodeID).ElapsedTime.Should().Be(0.25);
			processor.GraphContext.GraphVariables.TryGetArrayElement("history", 0, out int first).Should().BeTrue();
			first.Should().Be(1);

			processor.UpdateGraph(0.5);
			processor.GraphContext.GraphVariables.SetArrayElement("history", 0, 9);
		}
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_restore_runs_no_node_code_and_calls_no_completion()
	{
		var node = new TrackingStateNode();
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		int completions = 0;
		var processor = new GraphProcessor(graph) { OnGraphCompleted = () => completions++ };
		GraphSnapshot notStarted = processor.CaptureSnapshot();
		processor.StartGraph();
		GraphSnapshot running = processor.CaptureSnapshot();
		processor.StopGraph();

		processor.RestoreSnapshot(running);

		processor.GraphContext.IsActive.Should().BeTrue();
		processor.GraphContext.GetNodeContext<StateNodeContext>(node.NodeID).Active.Should().BeTrue();
		node.ActivateCount.Should().Be(1);
		completions.Should().Be(1);

		processor.RestoreSnapshot(notStarted);

		processor.GraphContext.IsActive.Should().BeFalse();
		node.DeactivateCount.Should().Be(1);
		completions.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_restored_run_carries_on_with_what_it_was_started_with()
	{
		var log = new List<string>();
		(GraphProcessor processor, _) = StartTimer(processor =>
		{
			processor.GraphContext.ActivationContext = "first";
			processor.GraphContext.SharedVariables = new Variables();
			processor.OnGraphCompleted = () => log.Add("first");
		});

		Variables? firstShared = processor.GraphContext.SharedVariables;
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		processor.StopGraph();

		processor.GraphContext.ActivationContext = "second";
		processor.GraphContext.SharedVariables = new Variables();
		processor.OnGraphCompleted = () => log.Add("second");
		processor.StartGraph();
		processor.UpdateGraph(1.0);

		processor.RestoreSnapshot(snapshot);

		processor.GraphContext.ActivationContext.Should().Be("first");
		processor.GraphContext.SharedVariables.Should().BeSameAs(firstShared);
		processor.UpdateGraph(1.0);
		log.Should().Equal("first", "second", "first");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Restored_activations_keep_their_ids_so_the_presentation_pass_shows_only_what_changed()
	{
		var log = new List<string>();
		var node = new PresentingNode(log);
		GraphProcessor processor = Start(node);
		processor.Present();
		GraphSnapshot snapshot = processor.CaptureSnapshot();

		Retrigger(processor, node);
		processor.Present();
		log.Should().Equal("start #1", "end #1", "start #2");

		// The restart runs again with the id it had the first time, and what it shows is already showing.
		processor.RestoreSnapshot(snapshot);
		Retrigger(processor, node);
		processor.Present();
		log.Should().HaveCount(3);

		processor.RestoreSnapshot(snapshot);
		processor.Present();
		log.Should().Equal("start #1", "end #1", "start #2", "end #2", "start #3");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Running_nodes_let_go_newest_first_and_take_hold_again_oldest_first()
	{
		var log = new List<string>();
		var first = new HoldingNode("first", log);
		var second = new HoldingNode("second", log);
		var third = new HoldingNode("third", log);
		var graph = new Graph();
		graph.AddNode(first);
		graph.AddNode(second);
		graph.AddNode(third);

		// Activated third, first, second: activation order rather than node order.
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			third.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			third.OutputPorts[HoldingNode.OnActivatePort],
			first.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			first.OutputPorts[HoldingNode.OnActivatePort],
			second.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		log.Clear();

		processor.RestoreSnapshot(snapshot);

		log.Should().Equal(
			"detach second",
			"detach first",
			"detach third",
			"attach third",
			"attach first",
			"attach second");
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_hook_that_throws_keeps_neither_the_others_nor_the_restore_from_completing()
	{
		var log = new List<string>();
		var failing = new HoldingNode("failing", log, throwOnDetach: true);
		var other = new HoldingNode("other", log);
		var graph = new Graph();
		graph.AddNode(failing);
		graph.AddNode(other);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			other.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			other.OutputPorts[HoldingNode.OnActivatePort],
			failing.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		log.Clear();

		processor.Invoking(x => x.RestoreSnapshot(snapshot))
			.Should().Throw<InvalidOperationException>().WithMessage("detach failing");

		log.Should().Equal("detach failing", "detach other", "attach other", "attach failing");
		processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Snapshots_are_refused_while_the_graph_is_running_node_code()
	{
		GraphProcessor? processor = null;
		var node = new CallbackStateNode(() => processor!.CaptureSnapshot());
		processor = Start(node);

		processor.Invoking(x => x.UpdateGraph(0.1)).Should().Throw<InvalidOperationException>();

		var log = new List<string>();
		var presenting = new PresentingNode(log, onPresent: graphProcessor => graphProcessor.CaptureSnapshot());
		GraphProcessor presentingProcessor = Start(presenting);
		presenting.Processor = presentingProcessor;

		presentingProcessor.Invoking(x => x.Present()).Should().Throw<InvalidOperationException>();
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void A_snapshot_from_another_processor_is_refused()
	{
		(GraphProcessor processor, _) = StartTimer();
		(GraphProcessor otherGraph, _) = StartTimer();
		var sameGraph = new GraphProcessor(processor.Graph);
		sameGraph.StartGraph();

		processor.Invoking(x => x.RestoreSnapshot(otherGraph.CaptureSnapshot())).Should().Throw<ArgumentException>();
		processor.Invoking(x => x.RestoreSnapshot(sameGraph.CaptureSnapshot())).Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Snapshots_are_refused_while_a_message_is_being_delivered()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("count", 0);
		var count = new IncrementCounterNode("count");
		graph.AddNode(count);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			count.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		graph.EntryNode.OutputPorts[EntryNode.OutputPort].OnEmitMessage += _ => processor.CaptureSnapshot();

		processor.Invoking(x => x.StartGraph()).Should().Throw<InvalidOperationException>();
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void The_update_stamp_counts_on_across_a_restore()
	{
		(GraphProcessor processor, _) = StartTimer();
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		processor.UpdateGraph(0.1);
		processor.UpdateGraph(0.1);
		ulong stamp = processor.GraphContext.UpdateStamp;

		processor.RestoreSnapshot(snapshot);

		processor.GraphContext.UpdateStamp.Should().BeGreaterThan(stamp);
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Shared_variables_are_left_to_whatever_owns_them()
	{
		var shared = new Variables();
		shared.DefineVariable("score", 1);
		(GraphProcessor processor, _) = StartTimer(processor => processor.GraphContext.SharedVariables = shared);
		GraphSnapshot snapshot = processor.CaptureSnapshot();
		var sharedSnapshot = new Variables();
		shared.CopyTo(sharedSnapshot);
		shared.SetVar("score", 2);

		processor.RestoreSnapshot(snapshot);

		shared.TryGetVar("score", out int score).Should().BeTrue();
		score.Should().Be(2, "shared variables belong to the entity, which snapshots them itself");

		sharedSnapshot.CopyTo(shared);
		shared.TryGetVar("score", out score).Should().BeTrue();
		score.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Snapshot")]
	public void Variables_copy_into_another_bag_without_sharing_their_arrays()
	{
		var source = new Variables();
		source.DefineVariable("health", 10);
		source.DefineArrayVariable("path", [new Variant128(1), new Variant128(2)]);
		source.DefineObjectVariable("target", "goblin");
		source.DefineObjectArrayVariable("names", ["a", "b"]);
		var destination = new Variables();
		destination.DefineVariable("stale", 1);

		source.CopyTo(destination);

		destination.TryGetVar("stale", out int _).Should().BeFalse();
		destination.TryGetVar("health", out int health).Should().BeTrue();
		health.Should().Be(10);
		destination.TryGetObject("target", out string? target).Should().BeTrue();
		target.Should().Be("goblin");

		destination.SetArrayElement("path", 0, 5);
		destination.SetObjectArrayElement("names", 0, "z");

		source.TryGetArrayElement("path", 0, out int step).Should().BeTrue();
		step.Should().Be(1);
		source.TryGetObjectArrayElement("names", 0, out string? name).Should().BeTrue();
		name.Should().Be("a");
	}

	private static (GraphProcessor Processor, TimerNode Timer) StartTimer(Action<GraphProcessor>? beforeStart = null)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);
		graph.VariableDefinitions.DefineVariable("count", 0);
		graph.VariableDefinitions.DefineArrayVariable("history", 1, 2);

		TimerNode timer = CreateTimerNode("duration");
		var count = new IncrementCounterNode("count");
		graph.AddNode(timer);
		graph.AddNode(count);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnTimerEndPort],
			count.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		beforeStart?.Invoke(processor);
		processor.StartGraph();
		return (processor, timer);
	}

	private static GraphProcessor Start(Node node)
	{
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private static void Retrigger(GraphProcessor processor, Node node)
	{
		node.InputPorts[ActionNode.InputPort].ReceiveMessage(processor.GraphContext);
	}

	private static int ReadCount(GraphProcessor processor)
	{
		processor.GraphContext.GraphVariables.TryGetVar("count", out int count).Should().BeTrue();
		return count;
	}

	private sealed class PresentingNode(List<string> log, Action<GraphProcessor>? onPresent = null)
		: StateNode<StateNodeContext>(restartOnRetrigger: true)
	{
		private readonly List<string> _log = log;

		private readonly Action<GraphProcessor>? _onPresent = onPresent;

		private int _starts;

		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		public GraphProcessor? Processor { get; set; }

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
			if (Processor is not null)
			{
				_onPresent?.Invoke(Processor);
			}

			string presentation = $"#{++_starts}";
			_log.Add($"start {presentation}");
			return presentation;
		}

		protected override void OnPresentationDeactivated(object? presentation)
		{
			_log.Add($"end {presentation}");
		}
	}

	private sealed class HoldingNode(string name, List<string> log, bool throwOnDetach = false)
		: StateNode<StateNodeContext>
	{
		private readonly string _name = name;

		private readonly List<string> _log = log;

		private readonly bool _throwOnDetach = throwOnDetach;

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnRestoreDetach(GraphContext graphContext)
		{
			_log.Add($"detach {_name}");

			if (_throwOnDetach)
			{
				throw new InvalidOperationException($"detach {_name}");
			}
		}

		protected override void OnRestoreAttach(GraphContext graphContext)
		{
			_log.Add($"attach {_name}");
		}
	}
}
