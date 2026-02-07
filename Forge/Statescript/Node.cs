// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Base class for all Nodes in the Statescript system.
/// </summary>
public abstract class Node
{
	private readonly List<InputPort> _inputPorts;
	private readonly List<OutputPort> _outputPorts;

	/// <summary>
	/// Gets or sets the unique identifier for this node.
	/// </summary>
	public Guid NodeID { get; set; }

	/// <summary>
	/// Gets the input ports of this node.
	/// </summary>
	public InputPort[]? InputPorts { get; private set; }

	/// <summary>
	/// Gets the output ports of this node.
	/// </summary>
	public OutputPort[]? OutputPorts { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Node"/> class.
	/// </summary>
	protected Node()
	{
		NodeID = Guid.NewGuid();
		_inputPorts = [];
		_outputPorts = [];
	}

	/// <summary>
	/// Adds an input port to this node.
	/// </summary>
	/// <param name="inputPort">The input port to add.</param>
	public void AddPort(InputPort inputPort)
	{
		_inputPorts.Add(inputPort);
		inputPort.SetOwnerNode(this);
	}

	/// <summary>
	/// Adds an output port to this node.
	/// </summary>
	/// <param name="outputPort">The output port to add.</param>
	public void AddPort(OutputPort outputPort)
	{
		_outputPorts.Add(outputPort);
	}

	/// <summary>
	/// Finalizes the initialization of ports for this node.
	/// </summary>
	/// <remarks>
	/// No more ports can be added after calling this method.
	/// </remarks>
	public void FinalizePortsInitialization()
	{
		InputPorts = [.. _inputPorts];
		OutputPorts = [.. _outputPorts];
	}

	internal void OnMessageReceived(
		InputPort receiverPort,
		Variables graphVariables,
		IGraphContext graphContext)
	{
		graphContext.InternalNodeActivationStatus[NodeID] = true;

		HandleMessage(receiverPort, graphVariables, graphContext);
	}

	internal void OnSubgraphDisabledMessageReceived(Variables graphVariables, IGraphContext graphContext)
	{
		Validation.Assert(OutputPorts is not null, "Output ports should have been initialized.");

		if (!graphContext.InternalNodeActivationStatus.TryAdd(NodeID, false))
		{
			if (!graphContext.InternalNodeActivationStatus[NodeID])
			{
				return;
			}

			graphContext.InternalNodeActivationStatus[NodeID] = false;
		}

		BeforeDisable(graphVariables, graphContext);

		foreach (OutputPort outputPort in OutputPorts)
		{
			outputPort.InternalEmitDisableSubgraphMessage(graphVariables, graphContext);
		}

		AfterDisable(graphVariables, graphContext);
	}

	/// <summary>
	/// Creates a port of the specified type with the given index.
	/// </summary>
	/// <typeparam name="T">The type of port to create.</typeparam>
	/// <param name="index">The index of the port.</param>
	/// <returns>The created port.</returns>
	protected static T CreatePort<T>(byte index)
		where T : Port, new()
	{
		return new T
		{
			Index = index,
		};
	}

	/// <summary>
	/// Emits a message from the specified output ports.
	/// </summary>
	/// <param name="graphVariables">The graph variables.</param>
	/// <param name="graphContext">The graph context.</param>
	/// <param name="portIds">The IDs of the output ports to emit the message from.</param>
	protected virtual void EmitMessage(Variables graphVariables, IGraphContext graphContext, params int[] portIds)
	{
		Validation.Assert(OutputPorts is not null, "Output ports should have been initialized.");

		foreach (var portId in portIds)
		{
			OutputPorts[portId].EmitMessage(graphVariables, graphContext);
		}
	}

	/// <summary>
	/// Handles an incoming message on the specified input port.
	/// </summary>
	/// <param name="receiverPort">The input port that received the message.</param>
	/// <param name="graphVariables">The graph variables.</param>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void HandleMessage(InputPort receiverPort, Variables graphVariables, IGraphContext graphContext)
	{
	}

	/// <summary>
	/// Called before the node is disabled.
	/// </summary>
	/// <param name="graphVariables">The graph variables.</param>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void BeforeDisable(Variables graphVariables, IGraphContext graphContext)
	{
	}

	/// <summary>
	/// Called after the node is disabled.
	/// </summary>
	/// <param name="graphVariables">The graph variables.</param>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void AfterDisable(Variables graphVariables, IGraphContext graphContext)
	{
	}
}
