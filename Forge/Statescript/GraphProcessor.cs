// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Provides functionality to execute and manage the lifecycle of a graph within a specified context.
/// </summary>
/// <remarks>
/// <para>The <see cref="GraphProcessor"/> class pairs a shared, immutable <see cref="Graph"/> definition with a
/// per-execution <see cref="GraphContext"/> that holds all mutable runtime state (variable values, node contexts,
/// activation flags). Multiple processors can share the same <see cref="Graph"/> instance, each with its own context
/// (Flyweight pattern).</para>
/// <para>A <see cref="GraphProcessor"/> is reusable: after a graph completes naturally or is explicitly stopped,
/// it can be started again with a fresh execution cycle.</para>
/// </remarks>
public class GraphProcessor
{
	private static readonly Comparison<Node> _byIndex = static (x, y) => x.Index.CompareTo(y.Index);

	private readonly List<Node> _updateBuffer = [];

	private readonly Dictionary<Node, Presented> _presented = [];

	private readonly List<Node> _presentBuffer = [];

	private bool _updating;

	private bool _presenting;

	/// <summary>
	/// Gets the graph that this processor is responsible for executing.
	/// </summary>
	public Graph Graph { get; }

	/// <summary>
	/// Gets the context in which the graph is executed. The context holds all mutable runtime state including variable
	/// values, node contexts, and activation status.
	/// </summary>
	public GraphContext GraphContext { get; }

	/// <summary>
	/// Gets or sets an optional callback that is invoked when the graph completes naturally (i.e., all state nodes
	/// have deactivated) or when the graph is explicitly stopped. This allows external systems to react to graph
	/// completion without polling.
	/// </summary>
	public Action? OnGraphCompleted { get; set; }

	/// <summary>
	/// Gets a value indicating whether something is still presented: a presentation node whose presentation
	/// <see cref="Present"/> has not ended yet, even if the graph has stopped since.
	/// </summary>
	public bool IsPresenting => _presented.Count > 0;

	// A run part way through changing - a pass walking it, a stop tearing it down, node code or a message on its way
	// back - has nothing settled for a snapshot to keep or to replace. A message of a run that has ended leaves its
	// count behind, which the next run starts over from.
	internal bool IsChanging => _updating
		|| _presenting
		|| GraphContext.IsStopping
		|| GraphContext.RunningFrames > 0
		|| (GraphContext.HasStarted && GraphContext.FinalizationDeferralCount > 0);

	/// <summary>
	/// Initializes a new instance of the <see cref="GraphProcessor"/> class.
	/// </summary>
	/// <param name="graph">The graph to be executed by this processor.</param>
	/// <param name="sharedVariables">Optional shared variables for this graph execution. When set, scope-aware variable
	/// resolvers such as <see cref="Properties.VariableResolver"/> can read entity-level shared state.</param>
	public GraphProcessor(Graph graph, Variables? sharedVariables = null)
	{
		Graph = graph;
		GraphContext = new GraphContext { SharedVariables = sharedVariables };
	}

	/// <summary>
	/// Starts the execution of the graph. This method initializes the context's runtime variables from the graph's
	/// variable definitions, ensuring that each execution instance has independent state, and then initiates the
	/// graph's entry node to begin processing.
	/// </summary>
	/// <remarks>
	/// Called while the graph is stopping - from a node's teardown inside <see cref="StopGraph"/> - or while a node's
	/// code is still running - a cue handler that ends the ability the graph runs for and starts it again - the graph
	/// starts once the stop has completed, <see cref="OnGraphCompleted"/> has run and that code has returned, unless it
	/// was started again or stopped by then.
	/// </remarks>
	/// <param name="variableOverrides">An optional callback invoked after variables are initialized from definitions
	/// but before the graph's entry node begins processing. Use this to overwrite specific variable values with
	/// runtime data (e.g., activation context from an ability).</param>
	public void StartGraph(Action<Variables>? variableOverrides = null)
	{
		if (GraphContext.StartMustWait)
		{
			GraphContext.PendingStart = () => StartGraph(variableOverrides);
			return;
		}

		GraphContext.PendingStart = null;
		GraphContext.Processor = this;
		GraphContext.HasStarted = true;
		GraphContext.RunStamp++;
		GraphContext.FinalizationDeferralCount = 0;
		GraphContext.GraphVariables.InitializeFrom(Graph.VariableDefinitions);
		variableOverrides?.Invoke(GraphContext.GraphVariables);
		Graph.FinalizeConnections();
		Graph.EntryNode.StartGraph(GraphContext);

		// If no state nodes were activated during the initial message propagation (e.g., action-only graphs), the graph
		// is already complete.
		if (GraphContext.HasStarted && !GraphContext.IsActive)
		{
			FinalizeGraph();
		}
	}

	/// <summary>
	/// Updates all active state nodes in the graph with the given delta time. Only state nodes that are currently
	/// active are updated, avoiding unnecessary iteration over inactive nodes. Call this method in your game loop to
	/// drive time-dependent state node logic such as timers, animations, or continuous evaluation.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	public void UpdateGraph(double deltaTime)
	{
		if (!BufferActiveNodes())
		{
			return;
		}

		ulong run = GraphContext.RunStamp;

		try
		{
			// A node can end the graph part way through the pass, and a graph started over from there has nodes of its
			// own that the rest of this delta must not reach.
			for (int i = 0; i < _updateBuffer.Count && GraphContext.RunStamp == run; i++)
			{
				_updateBuffer[i].Update(deltaTime, GraphContext);
			}
		}
		finally
		{
			_updating = false;
		}
	}

	/// <summary>
	/// Updates all active state nodes on the host's fixed step. Call this from the game's fixed callback - a physics
	/// step, or a network tick - alongside <see cref="UpdateGraph"/> in its frame callback.
	/// </summary>
	/// <remarks>
	/// A host with no fixed step of its own simply never calls this, and the nodes that need one stop running rather
	/// than running at the frame rate. Nothing in the graph requires both to be driven.
	/// </remarks>
	/// <param name="deltaTime">The length of the fixed step, in seconds.</param>
	public void FixedUpdateGraph(double deltaTime)
	{
		if (!BufferActiveNodes())
		{
			return;
		}

		ulong run = GraphContext.RunStamp;

		try
		{
			for (int i = 0; i < _updateBuffer.Count && GraphContext.RunStamp == run; i++)
			{
				_updateBuffer[i].FixedUpdate(deltaTime, GraphContext);
			}
		}
		finally
		{
			_updating = false;
		}
	}

	/// <summary>
	/// Stops the execution of the graph. This method calls the entry node's stop method to halt the graph's processing
	/// and then removes all node contexts from the graph context to clean up any state associated with the graph's
	/// execution. This method is safe to call re-entrantly (e.g., from an <see cref="ExitNode"/> triggered
	/// during the disable cascade).
	/// </summary>
	/// <remarks>
	/// A node that throws as it is ended keeps neither the others from ending nor the stop from completing:
	/// <see cref="OnGraphCompleted"/> runs and a start asked for during the stop follows as usual, even when one of
	/// them throws as well, and the first exception is rethrown after.
	/// </remarks>
	public void StopGraph()
	{
		// A start still waiting for node code to return is ended by a later stop, as it would have been had it run
		// first, even one that finds the graph already over. A stop asked for while the graph is still stopping is part
		// of that stop, which a start asked for during it follows.
		if (!GraphContext.IsStopping)
		{
			GraphContext.PendingStart = null;
		}

		if (GraphContext.Processor != this || !GraphContext.HasStarted)
		{
			return;
		}

		// Clear HasStarted first so the disable cascade is re-entrancy safe (e.g. an ExitNode triggering StopGraph, or
		// a state node reaching FinalizeGraph) without nulling Processor yet. Keeping Processor set throughout the
		// cascade lets action nodes on OnDeactivate paths still resolve property-backed inputs.
		GraphContext.HasStarted = false;
		GraphContext.RunStamp++;
		GraphContext.IsStopping = true;
		ExceptionDispatchInfo? failure = null;

		// The stop walks the graph afresh: a disabling still under way has already marked its node as passed, and the
		// stop would otherwise go no further than it, leaving that node and what lies below it never ended.
		GraphContext.InternalNodeActivationStatus.Clear();

		try
		{
			Graph.EntryNode.StopGraph(GraphContext);
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		// A node the walk did not reach - started from outside the graph's connections, or passed over when a teardown
		// threw - is ended here instead.
		Node[] unreached = [.. GraphContext.ActiveStateNodes];
		Array.Sort(unreached, _byIndex);

		foreach (Node node in unreached)
		{
			try
			{
				node.OnSubgraphDisabledMessageReceived(GraphContext);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		GraphContext.IsStopping = false;
		GraphContext.Processor = null;
		GraphContext.ActiveStateNodes.Clear();
		GraphContext.InternalNodeActivationStatus.Clear();
		GraphContext.RemoveAllNodeContext();

		try
		{
			OnGraphCompleted?.Invoke();
		}
		catch (Exception exception)
		{
			failure ??= ExceptionDispatchInfo.Capture(exception);
		}

		ExceptionDispatchInfo? startFailure = GraphContext.RunPendingStart();
		(failure ?? startFailure)?.Throw();
	}

	/// <summary>
	/// Runs the presentation pass: brings what the graph's <see cref="NodeNetworkKind.Presentation"/> state nodes show
	/// in line with which of them are running now. Hosts call it once at the end of each frame, after the frame's
	/// updates; a host with nothing to show, such as a dedicated server, never calls it.
	/// </summary>
	/// <remarks>
	/// <para>Only the net change since the last pass is presented. Presentations whose activation has ended, restarted
	/// or stopped with the graph end first, through <c>OnPresentationDeactivated</c>; then running activations not yet
	/// presented start, through <c>OnPresentationActivated</c>. Each group goes in node order. An activation that
	/// started and ended between two passes is never presented, and a presentation that ends its own activation as it
	/// starts is ended right away, in the same pass.</para>
	/// <para>A presentation that throws keeps neither the others from running nor itself from being ended later, and
	/// the first exception is rethrown after.</para>
	/// </remarks>
	public void Present()
	{
		// The buffers are reused per pass, so a nested pass would clear the walk in progress. Refused, like a nested
		// update.
		if (_presenting)
		{
			Validation.Fail(
				"A presentation pass was re-entered while one was already running, which would corrupt the walk in " +
				"progress. Run Present from the host's end of frame, never from inside a presentation.");

			return;
		}

		_presenting = true;
		ExceptionDispatchInfo? failure = null;

		try
		{
			_presentBuffer.Clear();
			_presentBuffer.AddRange(_presented.Keys);
			_presentBuffer.Sort(_byIndex);

			foreach (Node node in _presentBuffer)
			{
				Presented presented = _presented[node];

				if (node.TryGetActivation(GraphContext, out ulong activationId)
					&& activationId == presented.ActivationId)
				{
					continue;
				}

				_presented.Remove(node);

				try
				{
					node.EndPresentation(presented.Presentation);
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}
			}

			_presentBuffer.Clear();

			foreach (Node node in GraphContext.ActiveStateNodes)
			{
				if (node.NetworkKind == NodeNetworkKind.Presentation && !_presented.ContainsKey(node))
				{
					_presentBuffer.Add(node);
				}
			}

			_presentBuffer.Sort(_byIndex);

			foreach (Node node in _presentBuffer)
			{
				// A presentation started earlier in this pass can reach the graph through the engine - a signal it
				// emits - and end this node before its turn.
				if (!node.TryGetActivation(GraphContext, out ulong activationId))
				{
					continue;
				}

				// Recorded first, so one that throws part way is still ended later.
				_presented[node] = new Presented(activationId, null);

				try
				{
					_presented[node] = new Presented(activationId, node.StartPresentation(GraphContext));
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}

				// Starting can end the node's own activation too - a signal the engine emits as it starts - and what
				// was started would otherwise stay shown until the next pass.
				if (node.TryGetActivation(GraphContext, out ulong currentId) && currentId == activationId)
				{
					continue;
				}

				Presented presented = _presented[node];
				_presented.Remove(node);

				try
				{
					node.EndPresentation(presented.Presentation);
				}
				catch (Exception exception)
				{
					failure ??= ExceptionDispatchInfo.Capture(exception);
				}
			}
		}
		finally
		{
			_presenting = false;
		}

		failure?.Throw();
	}

	/// <summary>
	/// Takes a snapshot of the graph's run, to put it back later with <see cref="RestoreSnapshot"/>: which state nodes
	/// are running, a copy of every node context and of the graph variables, and what the run was started with.
	/// </summary>
	/// <remarks>
	/// <para>Node contexts are copied member by member, and each context type copies what its node changes in place
	/// (<see cref="StateNodeContext.OnCopied"/>). What the presentation pass shows is not part of the snapshot: after a
	/// restore, the next pass brings it in line with the restored run.</para>
	/// <para>Take it from the host, between updates, rather than from inside a node, an update or a presentation pass,
	/// where the run is part way through changing.</para>
	/// </remarks>
	/// <returns>The snapshot.</returns>
	/// <exception cref="InvalidOperationException">Thrown while the graph is updating, presenting or stopping, or
	/// while node code or a message is on its way back.</exception>
	public GraphSnapshot CaptureSnapshot()
	{
		ThrowIfChanging("taken");

		var nodeContexts = new List<GraphSnapshot.NodeContextCopy>(GraphContext.NodeContextCount);
		CaptureNodeContext(Graph.EntryNode, nodeContexts);

		foreach (Node node in Graph.Nodes)
		{
			CaptureNodeContext(node, nodeContexts);
		}

		Node[] activeNodes = [.. GraphContext.ActiveStateNodes];
		Array.Sort(activeNodes, _byIndex);

		var graphVariables = new Variables();
		GraphContext.GraphVariables.CopyTo(graphVariables);

		return new GraphSnapshot(
			this,
			GraphContext.HasStarted,
			GraphContext.ActivationCount,
			[.. nodeContexts],
			activeNodes,
			[.. GraphContext.InternalNodeActivationStatus],
			graphVariables,
			GraphContext.SharedVariables,
			GraphContext.ActivationContext,
			GraphContext.Random,
			OnGraphCompleted);
	}

	/// <summary>
	/// Puts the graph's run back as it was when <see cref="CaptureSnapshot"/> took <paramref name="snapshot"/>,
	/// silently: no node activates or deactivates, nothing is emitted, and <see cref="OnGraphCompleted"/> is not
	/// called, whether the restore starts the run again, ends it or changes it.
	/// </summary>
	/// <remarks>
	/// <para>Running nodes that the restore replaces let go of what they hold outside the graph, newest activation
	/// first, and the running nodes it brings back take hold of it again, oldest first, through <c>OnRestoreDetach</c>
	/// and <c>OnRestoreAttach</c> on <see cref="StateNode{T}"/>. A hook that throws keeps neither the others from
	/// running nor the restore from completing, and the first exception is rethrown after.</para>
	/// <para>Restored activations keep their ids, and new ones are numbered on from where the snapshot was taken, so
	/// running the same steps again starts the same activations with the same ids, and the presentation pass shows only
	/// what changed. <see cref="GraphContext.UpdateStamp"/> counts on rather than going back.</para>
	/// <para>The snapshot is left as it was and can be restored again. Restore it from the host, between updates, like
	/// <see cref="CaptureSnapshot"/>.</para>
	/// <para>Only the processor that took a snapshot can restore it: activation ids, stamps and what node contexts
	/// point at belong to that processor's run.</para>
	/// </remarks>
	/// <param name="snapshot">A snapshot taken from this processor.</param>
	/// <exception cref="ArgumentException">Thrown when the snapshot was taken from another processor.</exception>
	/// <exception cref="InvalidOperationException">Thrown while the graph is updating, presenting or stopping, or
	/// while node code or a message is on its way back.</exception>
	public void RestoreSnapshot(GraphSnapshot snapshot)
	{
		if (snapshot.Processor != this)
		{
			throw new ArgumentException("The snapshot was taken from another processor.", nameof(snapshot));
		}

		ThrowIfChanging("restored");

		ExceptionDispatchInfo? failure = null;

		// Newest first, so what one node holds on top of what another holds is let go of in reverse.
		foreach (Node node in RunningNodesByActivation(newestFirst: true))
		{
			try
			{
				node.DetachForRestore(GraphContext);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		SwapIn(snapshot);

		foreach (Node node in RunningNodesByActivation(newestFirst: false))
		{
			try
			{
				node.AttachForRestore(GraphContext);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		failure?.Throw();
	}

	// Puts the snapshot's run in place without running any node code. What the running nodes hold outside the graph is
	// let go of before and taken hold of again after, by RestoreSnapshot or by an entity restore.
	internal void SwapIn(GraphSnapshot snapshot)
	{
		GraphContext.RemoveAllNodeContext();

		foreach (GraphSnapshot.NodeContextCopy nodeContext in snapshot.NodeContexts)
		{
			GraphContext.SetNodeContext(nodeContext.Node.NodeID, nodeContext.Context.Copy());
		}

		GraphContext.ActiveStateNodes.Clear();
		GraphContext.ActiveStateNodes.UnionWith(snapshot.ActiveNodes);
		GraphContext.InternalNodeActivationStatus.Clear();

		foreach (KeyValuePair<Guid, bool> status in snapshot.ActivationStatus)
		{
			GraphContext.InternalNodeActivationStatus.Add(status.Key, status.Value);
		}

		snapshot.GraphVariables.CopyTo(GraphContext.GraphVariables);
		GraphContext.HasStarted = snapshot.HasStarted;
		GraphContext.Processor = snapshot.HasStarted ? this : null;
		GraphContext.FinalizationDeferralCount = 0;
		GraphContext.ActivationCount = snapshot.ActivationCount;
		GraphContext.SharedVariables = snapshot.SharedVariables;
		GraphContext.ActivationContext = snapshot.ActivationContext;
		GraphContext.Random = snapshot.Random;
		OnGraphCompleted = snapshot.OnGraphCompleted;

		// These only count up. A value cached against the update stamp, or code that held on to the run, belongs to the
		// run the restore replaced.
		GraphContext.RunStamp++;
		GraphContext.UpdateStamp++;
	}

	// Ends the run without running any node code, for an ability instance an entity restore takes away. The running
	// nodes have already let go of what they hold, and the next presentation pass ends what the run showed.
	internal void DropRun()
	{
		GraphContext.RemoveAllNodeContext();
		GraphContext.ActiveStateNodes.Clear();
		GraphContext.InternalNodeActivationStatus.Clear();
		GraphContext.HasStarted = false;
		GraphContext.Processor = null;
		GraphContext.FinalizationDeferralCount = 0;
		OnGraphCompleted = null;
		GraphContext.RunStamp++;
		GraphContext.UpdateStamp++;
	}

	// Each running state node, with the order it took hold in, for an entity restore to let go and take hold again
	// across the entity's graphs.
	internal void AddHolds(List<EntityHold> holds)
	{
		foreach (Node node in GraphContext.ActiveStateNodes)
		{
			if (GraphContext.TryGetNodeContext(node.NodeID, out INodeContext? context)
				&& context is StateNodeContext nodeContext)
			{
				holds.Add(new EntityHold(nodeContext.HoldOrder, this, node));
			}
		}
	}

	/// <summary>
	/// Finalizes the graph execution after all state nodes have naturally deactivated. Unlike <see cref="StopGraph"/>,
	/// this does not propagate disable messages through the entry node since all nodes have already deactivated through
	/// their normal lifecycle. This method clears remaining runtime state (node contexts, activation status) so the GC
	/// can reclaim memory.
	/// </summary>
	internal void FinalizeGraph()
	{
		if (!GraphContext.HasStarted)
		{
			return;
		}

		GraphContext.HasStarted = false;
		GraphContext.RunStamp++;
		GraphContext.Processor = null;
		GraphContext.InternalNodeActivationStatus.Clear();
		GraphContext.RemoveAllNodeContext();
		OnGraphCompleted?.Invoke();
	}

	// Snapshots the active nodes before either update walks them, because a node updated part way through can
	// deactivate itself or another and modify the set being walked. Stamping here rather than in each caller is what
	// makes the frame and fixed updates count as the separate passes they are.
	private bool BufferActiveNodes()
	{
		if (!GraphContext.HasStarted)
		{
			return false;
		}

		// The buffer is one list reused per call, so a nested pass would clear and refill the list the outer walk is
		// still indexing: some nodes updated twice, others skipped, silently and depending on set order. Nesting is
		// never useful here - updates cascade downward through messages, not by re-driving the graph - so it is
		// refused outright rather than made to work. Refusing also keeps a validation-disabled build safe: it drops
		// the nested pass instead of miscounting everyone's time.
		if (_updating)
		{
			Validation.Fail(
				"A graph update was re-entered while one was already running, which would corrupt the walk in " +
				"progress. Drive UpdateGraph and FixedUpdateGraph from the host's own callbacks, never from inside " +
				"a node or an ability behavior.");

			return false;
		}

		_updating = true;

		GraphContext.UpdateStamp++;

		// In node order rather than set order: the set's order follows the history of activations, which another
		// machine simulating the same graph need not share.
		_updateBuffer.Clear();
		_updateBuffer.AddRange(GraphContext.ActiveStateNodes);
		_updateBuffer.Sort(_byIndex);

		return true;
	}

	private void ThrowIfChanging(string action)
	{
		if (IsChanging)
		{
			throw new InvalidOperationException(
				$"A graph snapshot cannot be {action} while the graph is updating, presenting or stopping, or while " +
				"node code or a message is on its way back. Take and restore snapshots from the host, between " +
				"updates.");
		}
	}

	private void CaptureNodeContext(Node node, List<GraphSnapshot.NodeContextCopy> nodeContexts)
	{
		if (GraphContext.TryGetNodeContext(node.NodeID, out INodeContext? context)
			&& context is StateNodeContext stateNodeContext)
		{
			nodeContexts.Add(new GraphSnapshot.NodeContextCopy(node, stateNodeContext.Copy()));
		}
	}

	private Node[] RunningNodesByActivation(bool newestFirst)
	{
		Node[] nodes = [.. GraphContext.ActiveStateNodes];
		ulong[] activationIds = new ulong[nodes.Length];

		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].TryGetActivation(GraphContext, out activationIds[i]);
		}

		Array.Sort(activationIds, nodes);

		if (newestFirst)
		{
			Array.Reverse(nodes);
		}

		return nodes;
	}

	private readonly record struct Presented(ulong ActivationId, object? Presentation);
}
