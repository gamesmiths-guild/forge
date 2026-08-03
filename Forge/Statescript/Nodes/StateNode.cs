// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// Node representing a state in the graph. It has input ports for activation and abortion, output ports for activation,
/// deactivation, and abortion events, as well as a subgraph output port.
/// </summary>
/// <typeparam name="T">The type of the state node context.</typeparam>
public abstract class StateNode<T> : Node
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
	/// Updates this state node with the given delta time. Only processes the update if the node is currently active.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	/// <param name="graphContext">The graph's context.</param>
#pragma warning disable SA1202 // Elements should be ordered by access
	internal override void Update(double deltaTime, GraphContext graphContext)
#pragma warning restore SA1202 // Elements should be ordered by access
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (!nodeContext.Active)
		{
			return;
		}

		OnUpdate(deltaTime, graphContext);
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
	/// Called once the node has finished activating, after <see cref="OnActivate"/>, after
	/// <see cref="OnActivatePort"/> and <see cref="SubgraphPort"/> have been emitted, and after any messages deferred
	/// during activation have been flushed. Not called when the node deactivated itself while activating.
	/// </summary>
	/// <remarks>
	/// <para>Use this instead of <see cref="OnActivate"/> for work that must emit messages <b>interleaved</b> with
	/// other state changes on the activation frame — a loop that writes an iteration variable before each emission,
	/// for example. Messages emitted from <see cref="OnActivate"/> are deferred and flushed as a batch afterwards, so
	/// any per-emission state written alongside them would already hold its final value by the time they fire.</para>
	/// <para>The node is guaranteed to be active when this is called, but anything reached from here can deactivate it
	/// or stop the graph. Implementations that emit more than once must re-check <see cref="IsNodeActive"/> between
	/// emissions.</para>
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	protected virtual void OnActivated(GraphContext graphContext)
	{
	}

	/// <summary>
	/// Checks whether this node is still active in the given context. Emitting a message can synchronously deactivate
	/// this node (an <see cref="AbortPort"/> message) or tear the whole graph down (an <see cref="ExitNode"/>, which
	/// discards every node context), so any node that emits repeatedly within a single call must re-check this between
	/// emissions instead of trusting a node context it captured earlier.
	/// </summary>
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
			var nodeContext = (StateNodeContext)graphContext.GetOrCreateNodeContext<T>(NodeID);

			nodeContext.WasAborted = false;
			nodeContext.Activating = true;
			ActivateNode(graphContext);
			OutputPorts[OnActivatePort].EmitMessage(graphContext);
			OutputPorts[SubgraphPort].EmitMessage(graphContext);
			nodeContext.Activating = false;

			HandleDeferredEmitMessages(graphContext, nodeContext);
			HandleDeferredDeactivationMessages(graphContext, nodeContext);

			if (IsNodeActive(graphContext))
			{
				OnActivated(graphContext);
			}
		}
		else if (receiverPort.Index == AbortPort)
		{
			if (graphContext.HasNodeContext(NodeID))
			{
				graphContext.GetNodeContext<StateNodeContext>(NodeID).WasAborted = true;
			}

			OutputPorts[OnAbortPort].EmitMessage(graphContext);
			DeactivateNode(graphContext);
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

		base.EmitMessage(graphContext, portIds);
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

		graphContext.FinalizationDeferralCount++;

		try
		{
			DeactivateNode(graphContext);

			for (int i = 0; i < eventPortIds.Length; i++)
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
		finally
		{
			graphContext.FinalizationDeferralCount--;
		}

		if (graphContext.HasStarted
			&& graphContext.FinalizationDeferralCount == 0
			&& graphContext.ActiveStateNodes.Count == 0)
		{
			graphContext.Processor?.FinalizeGraph();
		}
	}

	/// <summary>
	/// Deactivates the node without emitting any custom messages.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	protected void DeactivateNode(GraphContext graphContext)
	{
		BeforeDisable(graphContext);

		foreach (SubgraphPort subgraphPort in SubgraphPorts)
		{
			subgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		AfterDisable(graphContext);
	}

	/// <inheritdoc/>
	protected sealed override void BeforeDisable(GraphContext graphContext)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (!nodeContext.Active)
		{
			return;
		}

		nodeContext.Active = false;

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

		OnDeactivate(graphContext);

		if (graphContext.FinalizationDeferralCount == 0
			&& graphContext.ActiveStateNodes.Count == 0)
		{
			graphContext.Processor?.FinalizeGraph();
		}
	}

	private void ActivateNode(GraphContext graphContext)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
		nodeContext.Active = true;
		graphContext.ActiveStateNodes.Add(this);
		OnActivate(graphContext);
	}

	private void HandleDeferredEmitMessages(GraphContext graphContext, StateNodeContext nodeContext)
	{
		if (nodeContext.DeferredEmitMessageData.Count > 0)
		{
			foreach (int emitEvent in nodeContext.DeferredEmitMessageData)
			{
				OutputPorts[emitEvent].EmitMessage(graphContext);
			}

			nodeContext.DeferredEmitMessageData.Clear();
		}
	}

	private void HandleDeferredDeactivationMessages(GraphContext graphContext, StateNodeContext nodeContext)
	{
		if (nodeContext.DeferredDeactivationEventPortIds is not null)
		{
			DeactivateNodeAndEmitMessage(graphContext, nodeContext.DeferredDeactivationEventPortIds);
			nodeContext.DeferredDeactivationEventPortIds = null;
		}
	}
}
