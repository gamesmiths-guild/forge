// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
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
	/// Gets a description of the node type, which is used in editor tooltips and documentation.
	/// </summary>
	public virtual string Description => $"{GetType().Name.Replace("Node", string.Empty)} node.";

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
	/// Gets the input property declarations for this node. Each entry describes a named value the node reads at
	/// runtime. Use <see cref="BindInput"/> to assign the actual variable or property name.
	/// </summary>
	public InputProperty[] InputProperties { get; }

	/// <summary>
	/// Gets the output variable declarations for this node. Each entry describes a named variable the node writes at
	/// runtime. Use <see cref="BindOutput"/> to assign the actual variable name.
	/// </summary>
	public OutputVariable[] OutputVariables { get; }

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

		var inputProperties = new List<InputProperty>();
		var outputVariables = new List<OutputVariable>();

#pragma warning disable S1699 // Constructors should only call non-overridable methods
		DefineParameters(inputProperties, outputVariables);
#pragma warning restore S1699 // Constructors should only call non-overridable methods

		InputProperties = [.. inputProperties];
		OutputVariables = [.. outputVariables];
	}

	/// <summary>
	/// Binds an input property to a variable or property name. At runtime, the node will resolve this name through
	/// <see cref="GraphContext.TryResolve{T}"/> or <see cref="GraphContext.TryResolveVariant"/>.
	/// </summary>
	/// <param name="index">The zero-based index of the input property to bind (as declared in
	/// <see cref="DefineParameters"/>).</param>
	/// <param name="propertyName">The name of the graph variable or property to bind to.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
	public void BindInput(byte index, StringKey propertyName)
	{
		if (index >= InputProperties.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(index),
				$"Input index {index} is out of range. This node has {InputProperties.Length} input(s).");
		}

		InputProperties[index] = InputProperties[index] with { BoundName = propertyName };
	}

	/// <summary>
	/// Binds an output variable to a variable name with an explicit scope override. Use this overload when the target
	/// variable bag (graph vs shared) should be configurable at bind time rather than fixed by the node's schema.
	/// </summary>
	/// <param name="index">The zero-based index of the output variable to bind.</param>
	/// <param name="variableName">The name of the variable to bind to.</param>
	/// <param name="scope">Which <see cref="Variables"/> bag this output should write to.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
	public void BindOutput(byte index, StringKey variableName, VariableScope scope = VariableScope.Graph)
	{
		if (index >= OutputVariables.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(index),
				$"Output index {index} is out of range. This node has {OutputVariables.Length} output(s).");
		}

		OutputVariables[index] = OutputVariables[index] with { BoundName = variableName, Scope = scope };
	}

	internal void OnMessageReceived(
		InputPort receiverPort,
		GraphContext graphContext)
	{
		graphContext.InternalNodeActivationStatus[NodeID] = true;

		HandleMessage(receiverPort, graphContext);
	}

	internal void OnSubgraphDisabledMessageReceived(GraphContext graphContext)
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
	internal virtual void Update(double deltaTime, GraphContext graphContext)
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
	/// Defines the input properties and output variables for this node. Subclasses override this method to declare
	/// their parameter schema: the labels and expected types. The actual variable/property names are bound later via
	/// <see cref="BindInput"/> and <see cref="BindOutput"/>.
	/// </summary>
	/// <remarks>
	/// The default implementation declares no parameters, which is correct for nodes like
	/// <see cref="Nodes.EntryNode"/> and <see cref="Nodes.ExitNode"/> that have no data dependencies.
	/// </remarks>
	/// <param name="inputProperties">The list to add input property declarations to.</param>
	/// <param name="outputVariables">The list to add output variable declarations to.</param>
	protected virtual void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		// Intentionally left blank.
	}

	/// <summary>
	/// Emits a message from the specified output ports.
	/// </summary>
	/// <param name="graphContext">The graph context.</param>
	/// <param name="portIds">The IDs of the output ports to emit the message from.</param>
	protected virtual void EmitMessage(GraphContext graphContext, params int[] portIds)
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
	protected virtual void HandleMessage(InputPort receiverPort, GraphContext graphContext)
	{
	}

	/// <summary>
	/// Called before the node is disabled.
	/// </summary>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void BeforeDisable(GraphContext graphContext)
	{
	}

	/// <summary>
	/// Called after the node is disabled.
	/// </summary>
	/// <param name="graphContext">The graph context.</param>
	protected virtual void AfterDisable(GraphContext graphContext)
	{
	}
}
