// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that keeps exactly one of several state subgraphs active, selected by a resolved integer — a
/// graph-native state machine.
/// </summary>
/// <remarks>
/// <para>The state input must resolve to an <see langword="int"/> and is re-evaluated on activation and every update
/// tick. Out-of-range selectors are clamped to the valid state range.</para>
/// <para>When the selection changes, the previous state's subgraph is disabled, the new state's subgraph is
/// activated, the Current State output variable (<see langword="int"/>) is written, and
/// <see cref="OnStateChangedPort"/> is emitted. Entering the first state after activation counts as a change.</para>
/// <para>All state subgraphs are cleaned up when the node deactivates.</para>
/// </remarks>
/// <param name="stateCount">How many state subgraph ports the machine has.</param>
public class StateMachineNode(int stateCount = 2) : StateNode<StateMachineNodeContext>
{
	/// <summary>
	/// Input property index for the state selector.
	/// </summary>
	public const byte StateInput = 0;

	/// <summary>
	/// Output variable index for the currently active state.
	/// </summary>
	public const byte CurrentStateOutput = 0;

	/// <summary>
	/// Output port index for the event emitted when the active state changes.
	/// </summary>
	public const byte OnStateChangedPort = 4;

	/// <summary>
	/// Port index of the first state subgraph port; state <c>i</c> uses port <c>FirstStatePort + i</c>.
	/// </summary>
	public const byte FirstStatePort = 5;

	private readonly int _stateCount = (stateCount >= 1 && stateCount <= byte.MaxValue - FirstStatePort + 1)
		? stateCount
		: throw new ArgumentOutOfRangeException(
			nameof(stateCount),
			$"stateCount must be between 1 and {byte.MaxValue - FirstStatePort + 1}.");

	/// <inheritdoc/>
	public override string Description => "Keeps exactly one state subgraph active, selected by an integer input.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnStateChangedPort, "OnStateChanged"));

		for (int i = 0; i < _stateCount; i++)
		{
			outputPorts.Add(CreatePort<SubgraphPort>((byte)(FirstStatePort + i), $"State {i}"));
		}
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("State", typeof(int)));
		outputVariables.Add(new OutputVariable("Current State", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		StateMachineNodeContext nodeContext = graphContext.GetNodeContext<StateMachineNodeContext>(NodeID);
		nodeContext.CurrentState = null;

		EvaluateState(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		EvaluateState(graphContext);
	}

	private void EvaluateState(GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[StateInput].BoundName, out int state))
		{
			return;
		}

		state = Math.Clamp(state, 0, _stateCount - 1);

		StateMachineNodeContext nodeContext = graphContext.GetNodeContext<StateMachineNodeContext>(NodeID);

		if (nodeContext.CurrentState == state)
		{
			return;
		}

		int? previousState = nodeContext.CurrentState;
		nodeContext.CurrentState = state;

		if (previousState.HasValue)
		{
			var previousSubgraphPort = (SubgraphPort)OutputPorts[FirstStatePort + previousState.Value];
			previousSubgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		WriteCurrentStateOutput(graphContext, state);

		EmitMessage(graphContext, OnStateChangedPort, FirstStatePort + state);
	}

	private void WriteCurrentStateOutput(GraphContext graphContext, int state)
	{
		OutputVariable output = OutputVariables[CurrentStateOutput];

		if (output.BoundName == Core.StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetVar(output.BoundName, state);
	}
}
