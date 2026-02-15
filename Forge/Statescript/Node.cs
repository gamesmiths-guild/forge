// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Base class for all Nodes in the Statescript system.
/// </summary>
public abstract class Node
{
	/// <summary>
	/// Defines the input and output ports for this node by adding them to the provided lists.
	/// </summary>
	/// <remarks>
	/// Subclasses must override this method to declare their ports. The base class handles finalization.
	/// </remarks>
	/// <param name="inputPorts">The list to add input ports to.</param>
	/// <param name="outputPorts">The list to add output ports to.</param>
	protected abstract void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts);

	/// <summary>
	/// Gets or sets the unique identifier for this node.
	/// </summary>
	public Guid NodeID { get; set; }

	/// <summary>
	/// Gets the input ports of this node.
	/// </summary>
	public InputPort[] InputPorts { get; }

	/// <summary>
	/// Gets the output ports of this node.
	/// </summary>
	public OutputPort[] OutputPorts { get; }

	/// <summary>
	/// Gets the subgraph output ports of this node. This is a cached subset of <see cref="OutputPorts"/> containing
	/// only <see cref="SubgraphPort"/> instances, built once during construction to avoid runtime type checks.
	/// </summary>
	internal SubgraphPort[] SubgraphPorts { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Node"/> class.
	/// </summary>
	protected Node()
	{
		NodeID = Guid.NewGuid();

		var inputPorts = new List<InputPort>();
		var outputPorts = new List<OutputPort>();

#pragma warning disable S1699 // Constructors should only call non-overridable methods
		DefinePorts(inputPorts, outputPorts);
#pragma warning restore S1699 // Constructors should only call non-overridable methods

		foreach (InputPort inputPort in inputPorts)
		{
			inputPort.SetOwnerNode(this);
		}

		InputPorts = [.. inputPorts];
		OutputPorts = [.. outputPorts];
		SubgraphPorts = [.. OutputPorts.OfType<SubgraphPort>()];
	}

	internal void OnMessageReceived(
		InputPort receiverPort,
		IGraphContext graphContext)
	{
		graphContext.InternalNodeActivationStatus[NodeID] = true;

		HandleMessage(receiverPort, graphContext);
	}

	internal void OnSubgraphDisabledMessageReceived(IGraphContext graphContext)
	{
		if (!graphContext.InternalNodeActivationStatus.TryAdd(NodeID, false))
		{
			if (!graphContext.InternalNodeActivationStatus[NodeID])
			{
				return;
			}

			graphContext.InternalNodeActivationStatus[NodeID] = false;
		}

		BeforeDisable(graphContext);

		foreach (OutputPort outputPort in OutputPorts)
		{
			outputPort.InternalEmitDisableSubgraphMessage(graphContext);
		}

		AfterDisable(graphContext);
	}

	/// <summary>
	/// Returns the indices of output ports that may emit a message when the specified input port receives a message.
	/// Used at graph construction time for static loop detection.
	/// </summary>
	/// <remarks>
	/// <para>The default implementation returns all output ports for any input, which is a safe over-approximation.
	/// Subclasses should override this to provide precise mappings for accurate cycle detection.</para>
	/// </remarks>
	/// <param name="inputPortIndex">The index of the input port that received the message.</param>
	/// <returns>An enumerable of output port indices that may fire in response.</returns>
	internal virtual IEnumerable<int> GetReachableOutputPorts(byte inputPortIndex)
	{
		for (var i = 0; i < OutputPorts.Length; i++)
		{
			yield return i;
		}
	}

	/// <summary>
	/// Returns the output ports that emit <b>regular messages</b> (not disable-subgraph messages) when this node
	/// receives a disable-subgraph signal. Used at graph construction time for static loop detection.
	/// </summary>
	/// <remarks>
	/// <para>When a node receives a disable-subgraph signal, two things happen: (1) some output ports may emit regular
	/// messages (e.g., <c>OnDeactivatePort</c> on a <see cref="Nodes.StateNode{T}"/>), and (2) all output ports
	/// propagate the disable-subgraph signal downstream. This method returns only category (1): the ports that emit
	/// regular messages, which can trigger <see cref="HandleMessage"/> on downstream nodes.</para>
	/// <para>The default implementation returns empty (no regular messages emitted during disable), which is correct
	/// for <see cref="Nodes.ActionNode"/> and <see cref="Nodes.ConditionNode"/>.</para>
	/// </remarks>
	/// <returns>An enumerable of output port indices that emit regular messages during disable.</returns>
	internal virtual IEnumerable<int> GetMessagePortsOnDisable()
	{
		return [];
	}

	/// <summary>
	/// Updates this node with the given delta time. The default implementation does nothing.
	/// Override in subclasses that need per-frame or per-tick logic (e.g., <see cref="Nodes.StateNode{T}"/>).
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
	/// <param name="graphContext">The graph context.</param>
	internal virtual void Update(double deltaTime, IGraphContext graphContext)
	{
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
	/// <param name="graphContext">The graph context.</param>
	/// <param name="portIds">The IDs of the output ports to emit the message from.</param>
	protected virtual void EmitMessage(IGraphContext graphContext, params int[] portIds)
	{
		foreach (var portId in portIds)
		{
			OutputPorts[portId].EmitMessage(graphContext);
		}
	}

	/// <summary>
	/// Handles an incoming message on the specified input port.
	/// </summary>
	/// <param name="receiverPort">The input port that received the message.</param>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void HandleMessage(InputPort receiverPort, IGraphContext graphContext)
	{
	}

	/// <summary>
	/// Called before the node is disabled.
	/// </summary>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void BeforeDisable(IGraphContext graphContext)
	{
	}

	/// <summary>
	/// Called after the node is disabled.
	/// </summary>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void AfterDisable(IGraphContext graphContext)
	{
	}
}
