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

			for (var i = SubgraphPort + 1; i < OutputPorts.Length; i++)
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

			for (var i = 0; i < SubgraphPorts.Length; i++)
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

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		inputPorts.Add(CreatePort<InputPort>(InputPort));
		inputPorts.Add(CreatePort<InputPort>(AbortPort));
		outputPorts.Add(CreatePort<EventPort>(OnActivatePort));
		outputPorts.Add(CreatePort<EventPort>(OnDeactivatePort));
		outputPorts.Add(CreatePort<EventPort>(OnAbortPort));
		outputPorts.Add(CreatePort<SubgraphPort>(SubgraphPort));
	}

	/// <inheritdoc/>
	protected sealed override void HandleMessage(InputPort receiverPort, GraphContext graphContext)
	{
		if (receiverPort.Index == InputPort)
		{
			var nodeContext = (StateNodeContext)graphContext.GetOrCreateNodeContext<T>(NodeID);

			nodeContext.Activating = true;
			ActivateNode(graphContext);
			OutputPorts[OnActivatePort].EmitMessage(graphContext);
			OutputPorts[SubgraphPort].EmitMessage(graphContext);
			nodeContext.Activating = false;

			HandleDeferredEmitMessages(graphContext, nodeContext);
			HandleDeferredDeactivationMessages(graphContext, nodeContext);
		}
		else if (receiverPort.Index == AbortPort)
		{
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

		DeactivateNode(graphContext);

		for (var i = 0; i < eventPortIds.Length; i++)
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

		if (!nodeContext.Active)
		{
			return;
		}

		base.AfterDisable(graphContext);

		nodeContext.Active = false;
		graphContext.ActiveStateNodes.Remove(this);
		OnDeactivate(graphContext);

		if (graphContext.ActiveStateNodes.Count == 0)
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
			foreach (var emitEvent in nodeContext.DeferredEmitMessageData)
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
