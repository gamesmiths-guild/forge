// Copyright © Gamesmiths Guild.

using System.Diagnostics;
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
	private const byte InputPort = 0;
	private const byte AbortPort = 1;
	private const byte OnActivatePort = 0;
	private const byte OnDeactivatePort = 1;
	private const byte OnAbortPort = 2;
	private const byte SubgraphPort = 3;

	/// <summary>
	/// Called when the node is activated.
	/// </summary>
	/// <param name="graphVariables">The graph's variables.</param>
	/// <param name="graphContext">The graph's context.</param>
	protected abstract void OnActivate(Variables graphVariables, IGraphContext graphContext);

	/// <summary>
	/// Called when the node is deactivated.
	/// </summary>
	/// <param name="graphVariables">The graph's variables.</param>
	/// <param name="graphContext">The graph's context.</param>
	protected abstract void OnDeactivate(Variables graphVariables, IGraphContext graphContext);

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
	protected sealed override void HandleMessage(InputPort receiverPort, Variables graphVariables, IGraphContext graphContext)
	{
		if (receiverPort.Index == InputPort)
		{
			var nodeContext = (StateNodeContext)graphContext.GetOrCreateNodeContext<T>(NodeID);

			nodeContext.Activating = true;
			ActivateNode(graphVariables, graphContext);
			OutputPorts[OnActivatePort].EmitMessage(graphVariables, graphContext);
			OutputPorts[SubgraphPort].EmitMessage(graphVariables, graphContext);
			nodeContext.Activating = false;

			HandleDeferredEmitMessages(graphContext, nodeContext);
			HandleDeferredDeactivationMessages(graphVariables, graphContext, nodeContext);
		}
		else if (receiverPort.Index == AbortPort)
		{
			OutputPorts[OnAbortPort].EmitMessage(graphVariables, graphContext);
			DeactivateNode(graphVariables, graphContext);
		}
	}

	/// <inheritdoc/>
	protected override void EmitMessage(Variables graphVariables, IGraphContext graphContext, params int[] portIds)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (nodeContext.Activating)
		{
			foreach (var portId in portIds)
			{
				nodeContext.DeferredEmitMessageData.Add(new PortVariable(portId, (Variables)graphVariables.Clone()));
			}

			return;
		}

		base.EmitMessage(graphVariables, graphContext, portIds);
	}

	/// <summary>
	/// <para>
	/// This method should be used when you want to deactivate the node and Emit a message on custom event ports in
	/// case of success of failure. It's important to use this method because it grantees that the messages are fired
	/// in the right order.
	/// </para>
	/// <para>OutputPort[OutputOnDeactivatePortID] (OnDeactivate) will always be called upon node deactivation and
	/// should not be used here.</para>
	/// </summary>
	/// <param name="graphVariables">The graph's variables.</param>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="eventPortIds">ID of ports you want to Emit a message to.</param>
	protected void DeactivateNodeAndEmitMessage(Variables graphVariables, IGraphContext graphContext, params int[] eventPortIds)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);

		if (nodeContext.Activating)
		{
			nodeContext.DeferredDeactivationEventPortIds = eventPortIds;
			return;
		}

		DeactivateNode(graphVariables, graphContext);

		for (var i = 0; i < eventPortIds.Length; i++)
		{
			Debug.Assert(eventPortIds[i] > OnAbortPort, "DeactivateNodeAndEmitMessage should be used only with custom ports.");
			Debug.Assert(OutputPorts[eventPortIds[i]] is EventPort, "Only EventPorts can be used for deactivation events.");
			OutputPorts[eventPortIds[i]].EmitMessage(graphVariables, graphContext);
		}
	}

	/// <summary>
	/// Deactivates the node without emitting any custom messages.
	/// </summary>
	/// <param name="graphVariables">The graph's variables.</param>
	/// <param name="graphContext">The graph's context.</param>
	protected void DeactivateNode(Variables graphVariables, IGraphContext graphContext)
	{
		BeforeDisable(graphVariables, graphContext);

		foreach (OutputPort outputPort in OutputPorts)
		{
			if (outputPort is SubgraphPort subgraphPort)
			{
				subgraphPort.EmitDisableSubgraphMessage(graphVariables, graphContext);
			}
		}

		AfterDisable(graphVariables, graphContext);
	}

	/// <inheritdoc/>
	protected sealed override void BeforeDisable(Variables graphVariables, IGraphContext graphContext)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
		if (nodeContext is null)
		{
			return;
		}

		if (!nodeContext.Active)
		{
			return;
		}

		base.BeforeDisable(graphVariables, graphContext);

		OutputPorts[OnDeactivatePort].EmitMessage(graphVariables, graphContext);
		((SubgraphPort)OutputPorts[SubgraphPort]).EmitDisableSubgraphMessage(graphVariables, graphContext);
	}

	/// <inheritdoc/>
	protected sealed override void AfterDisable(Variables graphVariables, IGraphContext graphContext)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
		if (nodeContext is null)
		{
			return;
		}

		if (!nodeContext.Active)
		{
			return;
		}

		base.AfterDisable(graphVariables, graphContext);

		nodeContext.Active = false;
		graphContext.ActiveStateNodeCount--;
		OnDeactivate(graphVariables, graphContext);
	}

	private void ActivateNode(Variables graphVariables, IGraphContext graphContext)
	{
		StateNodeContext nodeContext = graphContext.GetNodeContext<StateNodeContext>(NodeID);
		nodeContext.Active = true;
		graphContext.ActiveStateNodeCount++;
		OnActivate(graphVariables, graphContext);
	}

	private void HandleDeferredEmitMessages(IGraphContext graphContext, StateNodeContext nodeContext)
	{
		if (nodeContext.DeferredEmitMessageData.Count > 0)
		{
			foreach (PortVariable emitEvent in nodeContext.DeferredEmitMessageData)
			{
				OutputPorts[emitEvent.PortId].EmitMessage(emitEvent.Variables, graphContext);
			}

			nodeContext.DeferredEmitMessageData.Clear();
		}
	}

	private void HandleDeferredDeactivationMessages(Variables graphVariables, IGraphContext graphContext, StateNodeContext nodeContext)
	{
		if (nodeContext.DeferredDeactivationEventPortIds is not null)
		{
			DeactivateNodeAndEmitMessage(graphVariables, graphContext, nodeContext.DeferredDeactivationEventPortIds);
			nodeContext.DeferredDeactivationEventPortIds = null;
		}
	}
}
