// Copyright © Gamesmiths Guild.

using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing a state in the graph. It has input ports for activation and abortion, output ports for activation,
/// deactivation, and abortion events, as well as a subgraph output port.
/// </summary>
/// <remarks>
/// A message that reaches the input while the node is already active - a retrigger - is ignored, unless the node was
/// built to restart (<see cref="RestartOnRetrigger"/>), in which case it starts over through <see cref="OnRestart"/>.
/// One that reaches it while the node is still starting or deactivating, or while its graph is stopping, is ignored
/// either way. An abort that reaches a node that is not running is ignored too.
/// </remarks>
/// <typeparam name="T">The type of the state node context.</typeparam>
/// <param name="restartOnRetrigger">Whether a retrigger restarts the node through <see cref="OnRestart"/> instead of
/// being ignored. Only a node that overrides <see cref="OnRestart"/> passes this on.</param>
public abstract class StateNode<T>(bool restartOnRetrigger = false) : Node
	where T : StateNodeContext, new()
{
	/// <summary>
	/// Port index for the input port.
	/// </summary>
#pragma warning disable RCS1158 // Static member in generic type should use a type parameter
	public const byte InputPort = 0;

	/// <summary>
	/// Port index for the abort port.
	/// </summary>
	public const byte AbortPort = 1;

	/// <summary>
	/// Port index for the on activate port.
	/// </summary>
	public const byte OnActivatePort = 0;

	/// <summary>
	/// Port index for the on deactivate port.
	/// </summary>
	public const byte OnDeactivatePort = 1;

	/// <summary>
	/// Port index for the on abort port.
	/// </summary>
	public const byte OnAbortPort = 2;

	/// <summary>
	/// Port index for the subgraph port.
	/// </summary>
	public const byte SubgraphPort = 3;
#pragma warning restore RCS1158 // Static member in generic type should use a type parameter

	/// <summary>
	/// Called when the node is activated.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	protected abstract void OnActivate(GraphContext graphContext);

	/// <summary>
	/// Called when the node is deactivated.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	protected abstract void OnDeactivate(GraphContext graphContext);

	/// <inheritdoc/>
	public override string Description => $"A {GetType().Name.Replace("Node", string.Empty)} state node.";

	/// <summary>
	/// Gets a value indicating whether a retrigger - a message reaching the input while the node is already active -
	/// restarts the node through <see cref="OnRestart"/>. When <see langword="false"/>, the default, a retrigger is
	/// ignored.
	/// </summary>
	public bool RestartOnRetrigger { get; } = restartOnRetrigger;

	/// <summary>
	/// Updates this state node with the given delta time. Only processes the update if the node is currently active,
	/// and did not start or restart during this update.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	/// <param name="graphContext">The graph's context.</param>
	internal override void Update(double deltaTime, GraphContext graphContext)
	{
		StateNodeContext? nodeContext = ContextToUpdate(graphContext);

		if (nodeContext is not null)
		{
			EnterFrame(graphContext, nodeContext);
			ExceptionDispatchInfo? failure = null;

			try
			{
				OnUpdate(deltaTime, graphContext);
			}
			catch (Exception exception)
			{
				failure = ExceptionDispatchInfo.Capture(exception);
			}

			ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
			(failure ?? exit)?.Throw();
		}
	}

	/// <summary>
	/// Updates this state node on the host's fixed step. Only processes the update if the node is currently active,
	/// and did not start or restart during this step.
	/// </summary>
	/// <param name="deltaTime">The length of the fixed step, in seconds.</param>
	/// <param name="graphContext">The graph's context.</param>
	internal override void FixedUpdate(double deltaTime, GraphContext graphContext)
	{
		StateNodeContext? nodeContext = ContextToUpdate(graphContext);

		if (nodeContext is not null)
		{
			EnterFrame(graphContext, nodeContext);
			ExceptionDispatchInfo? failure = null;

			try
			{
				OnFixedUpdate(deltaTime, graphContext);
			}
			catch (Exception exception)
			{
				failure = ExceptionDispatchInfo.Capture(exception);
			}

			ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
			(failure ?? exit)?.Throw();
		}
	}

	/// <inheritdoc/>
	internal override IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
	{
		if (inputPortIndex == InputPort)
		{
			// InputPort fires OnActivatePort and SubgraphPort directly, and may fire OnDeactivatePort and custom
			// EventPorts via deferred deactivation.
			yield return OnActivatePort;
			yield return OnDeactivatePort;
			yield return SubgraphPort;

			for (int i = SubgraphPort + 1; i < OutputPorts.Length; i++)
			{
				yield return i;
			}
		}
		else if (inputPortIndex == AbortPort)
		{
			// AbortPort fires OnAbortPort directly, then DeactivateNode fires OnDeactivatePort and all SubgraphPorts
			// via BeforeDisable.
			yield return OnDeactivatePort;
			yield return OnAbortPort;

			for (int i = 0; i < SubgraphPorts.Length; i++)
			{
				yield return SubgraphPorts[i].Index;
			}
		}
	}

	/// <inheritdoc/>
	internal override IEnumerable<int> GetMessagePortsOnDisable()
	{
		// BeforeDisable fires OnDeactivatePort.EmitMessage() as a regular message.
		yield return OnDeactivatePort;
	}

	/// <summary>
	/// Called every update tick while the node is active. Override this method to implement per-frame or per-tick logic
	/// such as timers, animations, or continuous state evaluation.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	/// <param name="graphContext">The graph's context.</param>
	protected virtual void OnUpdate(double deltaTime, GraphContext graphContext)
	{
	}

	/// <summary>
	/// Called on every fixed step while the node is active. Override this instead of <see cref="OnUpdate"/> for logic
	/// that has to advance at a rate agreed in advance rather than at whatever rate the machine renders: moving a
	/// body, steering a character, asking the physics world a question, or anything a networked peer has to be able to
	/// reproduce step for step.
	/// </summary>
	/// <remarks>
	/// <para>Both hooks exist because the two rates are different and neither substitutes for the other. The frame
	/// rate is whatever the machine manages and can drift far above or below the fixed rate, so a body driven from
	/// <see cref="OnUpdate"/> is pushed a different amount per second on a fast machine than on a slow one, and a
	/// physics query asked from there is asked several times about a world that has not changed, or not at all in the
	/// step where it did.</para>
	/// <para>The name says <em>fixed</em> rather than <em>physics</em> because the interval is the guarantee and
	/// physics is only the most common reason to want one: a dedicated server with physics switched off still runs
	/// this rail, and a networked simulation drives it from its own clock rather than from the engine's.</para>
	/// <para>A host that never drives the fixed step simply never calls this, and a node that overrides it stops
	/// running rather than running at the wrong rate - which is the honest failure, since silently falling back to
	/// the frame would reintroduce exactly what overriding this avoids. Timers, animations and anything counting
	/// wall-clock time belong in <see cref="OnUpdate"/>, whose delta is the one the player experiences.</para>
	/// </remarks>
	/// <param name="deltaTime">The length of the fixed step, in seconds.</param>
	/// <param name="graphContext">The graph's context.</param>
	protected virtual void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
	}

	/// <summary>
	/// Called once the node has finished activating or restarting, after <see cref="OnActivate"/> or
	/// <see cref="OnRestart"/>, after <see cref="OnActivatePort"/> and <see cref="SubgraphPort"/> have been emitted,
	/// and after any messages deferred during activation have been flushed. Not called when the node deactivated
	/// itself while activating.
	/// </summary>
	/// <remarks>
	/// <para>Use this instead of <see cref="OnActivate"/> for work that must emit messages <b>interleaved</b> with
	/// other state changes on the activation frame — a loop that writes an iteration variable before each emission,
	/// for example. Messages emitted from <see cref="OnActivate"/> are deferred and flushed as a batch afterwards, so
	/// any per-emission state written alongside them would already hold its final value by the time they fire.</para>
	/// <para>The node is guaranteed to be active when this is called, but anything reached from here can deactivate it
	/// or stop the graph. Implementations that emit more than once must re-check between emissions that the context
	/// they hold is still <see cref="StateNodeContext.Active"/>.</para>
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	protected virtual void OnActivated(GraphContext graphContext)
	{
	}

	/// <summary>
	/// Called in place of <see cref="OnActivate"/> when a retrigger reaches the node while it is active and it was
	/// built to restart (<see cref="RestartOnRetrigger"/>). Override it to start the node over; the default does
	/// nothing.
	/// </summary>
	/// <remarks>
	/// <para>No deactivation runs first: the node stays active throughout, so an override releases whatever the running
	/// activation holds - an applied effect, a spawned instance - before acquiring it again, or it is left behind.
	/// Releasing can reach the rest of the graph and end the node or stop the graph, so an override checks that the
	/// context it released from is still <see cref="StateNodeContext.Active"/> before acquiring again. A node whose
	/// activation only resets its own counters can simply call <see cref="OnActivate"/>.</para>
	/// <para>The rest of the restart runs like an activation: <see cref="OnActivatePort"/> and
	/// <see cref="SubgraphPort"/> emit again, messages emitted from here are deferred until they have, and
	/// <see cref="OnActivated"/> runs once it is complete. Like an activation, it is first updated on the next pass,
	/// so no time from before the retrigger counts toward the new run. The subgraph is retriggered rather than torn
	/// down, so each node in it follows its own <see cref="RestartOnRetrigger"/>; a node whose subgraph depends on what
	/// the restart replaces disables that subgraph here first, so it comes back fresh.</para>
	/// <para>A node that overrides this takes a <c>restartOnRetrigger</c> constructor parameter and passes it to the
	/// base constructor, which is how an editor knows to offer the choice.</para>
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	protected virtual void OnRestart(GraphContext graphContext)
	{
	}

	/// <summary>
	/// Checks whether this node is active in the graph's current run.
	/// </summary>
	/// <remarks>
	/// Code that carries on after reaching outside the node - emitting a message, applying or removing an effect -
	/// checks the context it held from before instead, whose <see cref="StateNodeContext.Active"/> turns false once the
	/// node ends or its run does. What it reached can deactivate the node (an <see cref="AbortPort"/> message) or end
	/// the graph (an <see cref="ExitNode"/>), and once the graph has started over, this finds the new run's context.
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	/// <returns><see langword="true"/> if the node context still exists and the node is still active; otherwise,
	/// <see langword="false"/>.</returns>
	protected bool IsNodeActive(GraphContext graphContext)
	{
		return graphContext.HasNodeContext(NodeID)
			&& graphContext.GetNodeContext<StateNodeContext>(NodeID).Active;
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort, "Input"));
		inputPorts.Add(CreatePort<InputPort>(AbortPort, "Abort"));
		outputPorts.Add(CreatePort<EventPort>(OnActivatePort, "OnActivate"));
		outputPorts.Add(CreatePort<EventPort>(OnDeactivatePort, "OnDeactivate"));
		outputPorts.Add(CreatePort<EventPort>(OnAbortPort, "OnAbort"));
		outputPorts.Add(CreatePort<SubgraphPort>(SubgraphPort, "Subgraph"));
	}

	/// <inheritdoc/>
	protected sealed override void HandleMessage(InputPort receiverPort, GraphContext graphContext)
	{
		if (receiverPort.Index == InputPort)
		{
			// Nothing starts in a graph that is stopping: the stop may already have passed the node, leaving it running
			// in a graph that is gone.
			if (graphContext.IsStopping)
			{
				return;
			}

			StateNodeContext nodeContext = graphContext.GetOrCreateNodeContext<T>(NodeID);

			// A retrigger. OnActivate expects to start from inactive, and running it again would lose whatever the
			// running activation holds, so it is ignored unless the node knows how to start over. One that reaches the
			// node while it is still starting is ignored either way, or the restart would run inside the activation it
			// starts over.
			if (nodeContext.Active)
			{
				if (RestartOnRetrigger && !nodeContext.Activating)
				{
					RunActivation(graphContext, nodeContext, restarting: true);
				}

				return;
			}

			// Still deactivating. Starting over in the middle of that would leave the rest of the deactivation to tear
			// down the new activation instead of the old one.
			if (nodeContext.Deactivating)
			{
				return;
			}

			// Code is still running for the activation that ended - the node aborted and started again from inside it -
			// so the start waits for that code to return: what it has left to do, such as removing the cue it was
			// applying, is then done before the new activation begins rather than to it.
			if (nodeContext.RunningFrames > 0)
			{
				nodeContext.StartHeld = true;
				return;
			}

			nodeContext.WasAborted = false;
			RunActivation(graphContext, nodeContext, restarting: false);
		}
		else if (receiverPort.Index == AbortPort)
		{
			if (!graphContext.HasNodeContext(NodeID))
			{
				return;
			}

			StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

			// Only a running node is aborted: one that never started, has ended or is still ending has nothing to
			// abort. A start still waiting for the code of its last activation to return is aborted all the same.
			if (!nodeContext.Active)
			{
				nodeContext.StartHeld = false;
				return;
			}

			nodeContext.WasAborted = true;
			EnterFrame(graphContext, nodeContext);
			ExceptionDispatchInfo? failure = null;

			try
			{
				OutputPorts[OnAbortPort].EmitMessage(graphContext);
			}
			catch (Exception exception)
			{
				failure = ExceptionDispatchInfo.Capture(exception);
			}

			// OnAbort can end this node or the graph first. A handler that throws still lets the abort end the node
			// rather than leave it running as aborted, and a start sent to it from OnAbort waits for that.
			ExceptionDispatchInfo? deactivation = nodeContext.Active ? Deactivate(graphContext) : null;
			ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
			(failure ?? deactivation ?? exit)?.Throw();
		}
	}

	/// <inheritdoc/>
	protected override void EmitMessage(GraphContext graphContext, params int[] portIds)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (nodeContext.Activating)
		{
			nodeContext.DeferredEmitMessageData.AddRange(portIds);

			return;
		}

		EnterFrame(graphContext, nodeContext);
		ExceptionDispatchInfo? failure = null;

		try
		{
			// A port whose message ends this node, or the graph, ends the ones after it too: a subgraph started after
			// that would run under a node that is already gone.
			for (int i = 0; i < portIds.Length && nodeContext.Active; i++)
			{
				OutputPorts[portIds[i]].EmitMessage(graphContext);
			}
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
		(failure ?? exit)?.Throw();
	}

	/// <summary>
	/// Deactivates the node and emits messages through the specified event ports.
	/// </summary>
	/// <remarks>
	/// <para>If the node is currently in the process of activating, the deactivation and message emissions will be
	/// deferred until activation is complete. This prevents race conditions during the activation process.</para>
	/// <para>Use this method because it guarantees that the messages are fired in the right order.</para>
	/// <para>OutputPort[OnDeactivatePort] (OnDeactivate) will always be called upon node deactivation and should not be
	/// used here.</para>
	/// <para>The messages are emitted even when the deactivation throws, since the node has still ended, and the
	/// exception propagates once they have been.</para>
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="eventPortIds">ID of ports you want to Emit a message to.</param>
	protected void DeactivateNodeAndEmitMessage(GraphContext graphContext, params int[] eventPortIds)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (nodeContext.Activating)
		{
			nodeContext.DeferredDeactivationEventPortIds = eventPortIds;
			return;
		}

		ulong run = graphContext.RunStamp;
		graphContext.FinalizationDeferralCount++;

		// A node whose cleanup throws has still ended, and still reports how, so what waits on that report goes on.
		ExceptionDispatchInfo? failure = Deactivate(graphContext);

		try
		{
			// A port whose message ends the graph ends the ones after it too.
			for (int i = 0; i < eventPortIds.Length && graphContext.RunStamp == run; i++)
			{
				Validation.Assert(
					eventPortIds[i] > OnAbortPort,
					"DeactivateNodeAndEmitMessage should be used only with custom ports.");
				Validation.Assert(
					OutputPorts[eventPortIds[i]] is EventPort,
					"Only EventPorts can be used for deactivation events.");
				OutputPorts[eventPortIds[i]].EmitMessage(graphContext);
			}
		}
		catch (Exception exception)
		{
			failure ??= ExceptionDispatchInfo.Capture(exception);
		}

		// A run that ended along the way took its count with it, and the next one keeps its own. One still going
		// completes once nothing is left running, even if an emission threw, whose exception then propagates rather
		// than the completion's.
		if (graphContext.RunStamp == run)
		{
			graphContext.FinalizationDeferralCount--;
			ExceptionDispatchInfo? completion = graphContext.FinalizeIfIdle();
			failure ??= completion;
		}

		failure?.Throw();
	}

	/// <summary>
	/// Deactivates the node without emitting any custom messages.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	protected void DeactivateNode(GraphContext graphContext)
	{
		Deactivate(graphContext)?.Throw();
	}

	/// <inheritdoc/>
	protected sealed override void BeforeDisable(GraphContext graphContext)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		// A node that has ended has nothing to disable, but a start still waiting for the code of its last activation
		// to return is disabled with it.
		if (!nodeContext.Active)
		{
			nodeContext.StartHeld = false;
			return;
		}

		nodeContext.Active = false;
		nodeContext.Deactivating = true;

		base.BeforeDisable(graphContext);

		OutputPorts[OnDeactivatePort].EmitMessage(graphContext);
	}

	/// <inheritdoc/>
	protected sealed override void AfterDisable(GraphContext graphContext)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (nodeContext.Active)
		{
			return;
		}

		if (!graphContext.ActiveStateNodes.Remove(this))
		{
			return;
		}

		base.AfterDisable(graphContext);

		// A cleanup that throws has still ended the node: it can be started again rather than ignore every start for
		// the rest of the run, and a graph it was the last one running in completes.
		EnterFrame(graphContext, nodeContext);
		ExceptionDispatchInfo? failure = null;

		try
		{
			OnDeactivate(graphContext);
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		nodeContext.Deactivating = false;
		ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
		(failure ?? exit)?.Throw();
	}

	// Counts the code running for an activation on its node and on the graph, so that a start of either waits for that
	// code to return instead of having what it has left to do land on the new activation or run.
	private protected static void EnterFrame(GraphContext graphContext, StateNodeContext nodeContext)
	{
		nodeContext.RunningFrames++;
		graphContext.RunningFrames++;
	}

	// Everything that waited for the code to return runs, even when some of it throws, and the first failure is handed
	// back for the caller to rethrow unless its own code failed first, so what threw first is what propagates.
	private protected ExceptionDispatchInfo? ExitFrame(GraphContext graphContext, StateNodeContext nodeContext)
	{
		nodeContext.RunningFrames--;
		graphContext.RunningFrames--;
		ExceptionDispatchInfo? restart = null;

		// The start that waited for this activation's code follows once it has returned, unless the node's context went
		// with its run in the meantime.
		if (nodeContext.RunningFrames == 0 && nodeContext.StartHeld)
		{
			nodeContext.StartHeld = false;

			if (graphContext.HasNodeContext(NodeID)
				&& graphContext.GetNodeContext<StateNodeContext>(NodeID) == nodeContext)
			{
				try
				{
					InputPorts[InputPort].ReceiveMessage(graphContext);
				}
				catch (Exception exception)
				{
					restart = ExceptionDispatchInfo.Capture(exception);
				}
			}
		}

		// A run whose nodes all ended while code was still running completes once it has returned, and a start of the
		// graph that waited for that code follows.
		ExceptionDispatchInfo? completion = graphContext.FinalizeIfIdle();
		ExceptionDispatchInfo? start = graphContext.RunPendingStart();

		return restart ?? completion ?? start;
	}

	// A node that starts during a pass, restarted or ended and started again by one updated before it, would otherwise
	// count that pass's delta - time from before it started - toward the new run. Waiting for the next pass starts its
	// clock where an activation between passes starts it.
	private StateNodeContext? ContextToUpdate(GraphContext graphContext)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return null;
		}

		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		return nodeContext.Active && nodeContext.ActivationStamp != graphContext.UpdateStamp ? nodeContext : null;
	}

	private void RunActivation(GraphContext graphContext, StateNodeContext nodeContext, bool restarting)
	{
		EnterFrame(graphContext, nodeContext);
		ExceptionDispatchInfo? failure = null;

		try
		{
			nodeContext.Activating = true;
			nodeContext.ActivationStamp = graphContext.UpdateStamp;

			if (restarting)
			{
				OnRestart(graphContext);
			}
			else
			{
				ActivateNode(graphContext);
			}

			// The node's own work can end it or stop the whole graph - an effect cancelling the ability this graph
			// runs for - and so can whatever OnActivate reaches. Nothing is emitted for a node that is already gone: a
			// subgraph started under one would run with nothing left to disable it.
			if (nodeContext.Active)
			{
				OutputPorts[OnActivatePort].EmitMessage(graphContext);
			}

			if (nodeContext.Active)
			{
				OutputPorts[SubgraphPort].EmitMessage(graphContext);
			}

			nodeContext.Activating = false;

			HandleDeferredEmitMessages(graphContext, nodeContext);
			HandleDeferredDeactivationMessages(graphContext, nodeContext);

			if (nodeContext.Active)
			{
				OnActivated(graphContext);
			}
		}
		catch (Exception exception)
		{
			// An activation that throws is over all the same: the node emits and ends as usual from then on, and what
			// the activation deferred goes with it rather than being left for the next one.
			nodeContext.Activating = false;
			nodeContext.DeferredEmitMessageData.Clear();
			nodeContext.DeferredDeactivationEventPortIds = null;
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		ExceptionDispatchInfo? exit = ExitFrame(graphContext, nodeContext);
		(failure ?? exit)?.Throw();
	}

	private void ActivateNode(GraphContext graphContext)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
		nodeContext.Active = true;
		graphContext.ActiveStateNodes.Add(this);
		OnActivate(graphContext);
	}

	// A deactivation that throws anywhere still disables the node's whole subgraph, so nothing is left running under
	// it, and lets the node finish deactivating rather than leave it part way through and ignoring every start. What
	// threw first is handed back.
	private ExceptionDispatchInfo? Deactivate(GraphContext graphContext)
	{
		ulong run = graphContext.RunStamp;
		ExceptionDispatchInfo? failure = null;

		try
		{
			BeforeDisable(graphContext);
		}
		catch (Exception exception)
		{
			failure = ExceptionDispatchInfo.Capture(exception);
		}

		// OnDeactivate can end the graph, which deactivates everything this would have, and a graph started over from
		// there has nodes of its own.
		for (int i = 0; i < SubgraphPorts.Length && graphContext.RunStamp == run; i++)
		{
			try
			{
				SubgraphPorts[i].EmitDisableSubgraphMessage(graphContext);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		try
		{
			AfterDisable(graphContext);
		}
		catch (Exception exception)
		{
			failure ??= ExceptionDispatchInfo.Capture(exception);
		}

		return failure;
	}

	// What was deferred belongs to the activation, so a node aborted or a graph stopped along the way drops the rest: a
	// node that has already ended must not keep emitting, or report a second ending, or reach for a discarded context.
	private void HandleDeferredEmitMessages(GraphContext graphContext, StateNodeContext nodeContext)
	{
		List<int> deferred = nodeContext.DeferredEmitMessageData;

		for (int i = 0; i < deferred.Count && nodeContext.Active; i++)
		{
			OutputPorts[deferred[i]].EmitMessage(graphContext);
		}

		deferred.Clear();
	}

	private void HandleDeferredDeactivationMessages(GraphContext graphContext, StateNodeContext nodeContext)
	{
		int[]? eventPortIds = nodeContext.DeferredDeactivationEventPortIds;
		nodeContext.DeferredDeactivationEventPortIds = null;

		if (eventPortIds is not null && nodeContext.Active)
		{
			DeactivateNodeAndEmitMessage(graphContext, eventPortIds);
		}
	}
}
