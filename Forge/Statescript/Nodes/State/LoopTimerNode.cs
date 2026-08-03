// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that emits a repeating interval event while active, optionally deactivating after a configured
/// number of loops.
/// </summary>
/// <remarks>
/// <para>The interval input must resolve to a <see langword="double"/> value representing seconds, and is re-resolved
/// every update so it can be driven by attributes or other dynamic values. Non-positive intervals pause the timer.
/// </para>
/// <para>The loop-count input must resolve to an <see langword="int"/>. When unbound or non-positive, the timer loops
/// until the node is deactivated externally.</para>
/// <para>When more than one interval elapses in a single update, <see cref="OnIntervalPort"/> is emitted once per
/// completed interval. If a handler aborts this node or stops the graph, the remaining intervals of that update are
/// dropped.</para>
/// <para>When the configured number of loops completes, the node emits a final <see cref="OnIntervalPort"/> followed
/// by <see cref="OnFinishedPort"/> and deactivates itself.</para>
/// </remarks>
public class LoopTimerNode : StateNode<LoopTimerNodeContext>
{
	/// <summary>
	/// Input property index for the interval duration.
	/// </summary>
	public const byte IntervalInput = 0;

	/// <summary>
	/// Input property index for the number of loops to run.
	/// </summary>
	public const byte LoopCountInput = 1;

	/// <summary>
	/// Output port index for the event emitted every completed interval.
	/// </summary>
	public const byte OnIntervalPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the configured number of loops completes.
	/// </summary>
	public const byte OnFinishedPort = 5;

	/// <inheritdoc/>
	public override string Description => "Emits an event every interval while active, optionally deactivating after " +
		"a configured number of loops.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnIntervalPort, "OnInterval"));
		outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Interval", typeof(double)));
		inputProperties.Add(new InputProperty("Loop Count", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		LoopTimerNodeContext nodeContext = graphContext.GetNodeContext<LoopTimerNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.CompletedLoops = 0;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[IntervalInput].BoundName, out double interval)
			|| interval <= 0)
		{
			return;
		}

		if (!graphContext.TryResolve(InputProperties[LoopCountInput].BoundName, out int loopCount))
		{
			loopCount = 0;
		}

		LoopTimerNodeContext nodeContext = graphContext.GetNodeContext<LoopTimerNodeContext>(NodeID);

		nodeContext.ElapsedTime += deltaTime;

		while (nodeContext.ElapsedTime >= interval)
		{
			nodeContext.ElapsedTime -= interval;
			nodeContext.CompletedLoops++;

			if (loopCount > 0 && nodeContext.CompletedLoops >= loopCount)
			{
				DeactivateNodeAndEmitMessage(graphContext, OnIntervalPort, OnFinishedPort);
				return;
			}

			EmitMessage(graphContext, OnIntervalPort);

			// An interval handler can abort this node or stop the graph entirely, which invalidates the node context
			// this loop is accumulating into.
			if (!IsNodeActive(graphContext))
			{
				return;
			}
		}
	}
}
