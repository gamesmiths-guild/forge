// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that continuously evaluates a boolean condition while active, emitting events and routing subgraphs
/// on transitions.
/// </summary>
/// <remarks>
/// <para>The condition input must resolve to a <see langword="bool"/>. It is evaluated on activation (unless
/// <paramref name="initialCheckOnActivate"/> is disabled) and once per update tick while the node is active.</para>
/// <para>On every transition the node emits <see cref="OnBecameTruePort"/> or <see cref="OnBecameFalsePort"/>. The
/// first evaluation after activation counts as a transition into the evaluated value.</para>
/// <para>The <see cref="TrueSubgraphPort"/> and <see cref="FalseSubgraphPort"/> subgraphs mirror the condition: while
/// the condition holds, the matching subgraph is active; when the condition flips, the previous subgraph is disabled
/// and the other one is activated. Both subgraphs are cleaned up when the node deactivates.</para>
/// <para>With <paramref name="deactivateWhenTrue"/> enabled the node acts as a "wait until": when the condition
/// becomes <see langword="true"/> the node emits <see cref="OnBecameTruePort"/> and deactivates itself.</para>
/// </remarks>
/// <param name="deactivateWhenTrue">Whether the node deactivates itself when the condition becomes
/// <see langword="true"/>.</param>
/// <param name="initialCheckOnActivate">Whether the condition is evaluated immediately on activation instead of
/// waiting for the first update tick.</param>
public class ConditionMonitorNode(bool deactivateWhenTrue = false, bool initialCheckOnActivate = true)
	: StateNode<ConditionMonitorNodeContext>
{
	/// <summary>
	/// Input property index for the monitored condition.
	/// </summary>
	public const byte ConditionInput = 0;

	/// <summary>
	/// Output port index for the event emitted when the condition becomes true.
	/// </summary>
	public const byte OnBecameTruePort = 4;

	/// <summary>
	/// Output port index for the event emitted when the condition becomes false.
	/// </summary>
	public const byte OnBecameFalsePort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the condition is true.
	/// </summary>
	public const byte TrueSubgraphPort = 6;

	/// <summary>
	/// Output port index for the subgraph that is active while the condition is false.
	/// </summary>
	public const byte FalseSubgraphPort = 7;

	private readonly bool _deactivateWhenTrue = deactivateWhenTrue;
	private readonly bool _initialCheckOnActivate = initialCheckOnActivate;

	/// <inheritdoc/>
	public override string Description => "Monitors a boolean condition while active, emitting transition events and " +
		"routing between a true subgraph and a false subgraph.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnBecameTruePort, "OnBecameTrue"));
		outputPorts.Add(CreatePort<EventPort>(OnBecameFalsePort, "OnBecameFalse"));
		outputPorts.Add(CreatePort<SubgraphPort>(TrueSubgraphPort, "TrueSubgraph"));
		outputPorts.Add(CreatePort<SubgraphPort>(FalseSubgraphPort, "FalseSubgraph"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Condition", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		ConditionMonitorNodeContext nodeContext =
			graphContext.GetNodeContext<ConditionMonitorNodeContext>(NodeID);
		nodeContext.LastConditionValue = null;

		if (_initialCheckOnActivate)
		{
			EvaluateCondition(graphContext);
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		EvaluateCondition(graphContext);
	}

	private void EvaluateCondition(GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[ConditionInput].BoundName, out bool condition))
		{
			return;
		}

		ConditionMonitorNodeContext nodeContext =
			graphContext.GetNodeContext<ConditionMonitorNodeContext>(NodeID);

		if (nodeContext.LastConditionValue == condition)
		{
			return;
		}

		bool hadValue = nodeContext.LastConditionValue.HasValue;
		nodeContext.LastConditionValue = condition;

		if (condition && _deactivateWhenTrue)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnBecameTruePort);
			return;
		}

		if (hadValue)
		{
			var previousSubgraphPort = (SubgraphPort)OutputPorts[condition ? FalseSubgraphPort : TrueSubgraphPort];
			previousSubgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		if (condition)
		{
			EmitMessage(graphContext, OnBecameTruePort, TrueSubgraphPort);
		}
		else
		{
			EmitMessage(graphContext, OnBecameFalsePort, FalseSubgraphPort);
		}
	}
}
