// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Base class for state nodes that walk a bounded sequence, emitting <see cref="OnIterationPort"/> once per iteration
/// and one of three mutually exclusive endings when it stops.
/// </summary>
/// <remarks>
/// <para>The first iteration always runs on the activation frame. When the interval input is unbound or resolves to a
/// non-positive value the whole loop runs there too, one iteration after another, which is what makes same-frame
/// chains and burst patterns expressible. A positive interval instead spaces out the iterations that follow the first
/// one, so the synchronous behavior is simply the zero-interval limit of the paced one. This is deliberately unlike
/// <see cref="LoopTimerNode"/>, which never fires on the activation frame.</para>
/// <para>The optional condition input is evaluated once per iteration, right before that iteration is due, including
/// the first: the loop ends as soon as it does not hold. Leaving it unbound means the loop has no early exit.</para>
/// <para><b>Every way a loop can end has its own port</b>, and exactly one of them fires:</para>
/// <list type="bullet">
/// <item><description><see cref="OnFinishedPort"/> — the sequence ran out. The loop completed.</description></item>
/// <item><description><see cref="OnConditionFailedPort"/> — the condition stopped holding and cut the loop short,
/// possibly before its first iteration.</description></item>
/// <item><description><see cref="StateNode{T}.OnAbortPort"/> — the node was aborted from outside, through its abort
/// input.</description></item>
/// </list>
/// <para><see cref="StateNode{T}.OnDeactivatePort"/> still fires for all three, so a graph that only cares that the
/// loop is over routes that instead of wiring each ending.</para>
/// <para>A loop finishes on the same tick as its final iteration, so a count of three paced at 0.2 s ends at 0.4 s
/// rather than 0.6 s. A condition that stops holding is discovered when the next iteration comes due, which in a
/// paced loop is one interval later — the same way a guarded loop with a delay in its body behaves.</para>
/// <para>Writing to the bound index output from inside an iteration cannot steer the loop: how far it has walked
/// lives in <see cref="IterationNodeContext.NextIndex"/>, never read back from the variable, which the next iteration
/// simply overwrites.</para>
/// <para>Derived nodes supply the sequence through <see cref="DefineSourceParameters"/>, <see cref="HasIteration"/>
/// and <see cref="PrepareIteration"/>. Their source input always occupies index 0, because the shared condition and
/// interval inputs are appended after it.</para>
/// </remarks>
/// <typeparam name="T">The type of the iteration node context.</typeparam>
public abstract class IterationNode<T> : StateNode<T>
	where T : IterationNodeContext, new()
{
#pragma warning disable RCS1158 // Static member in generic type should use a type parameter
	/// <summary>
	/// Input property index for the optional loop condition.
	/// </summary>
	public const byte ConditionInput = 1;

	/// <summary>
	/// Input property index for the optional interval between iterations.
	/// </summary>
	public const byte IntervalInput = 2;

	/// <summary>
	/// Output port index for the event emitted once per iteration.
	/// </summary>
	public const byte OnIterationPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the sequence runs out, just before self-deactivation. Does not
	/// fire for a loop cut short by its condition, which emits <see cref="OnConditionFailedPort"/> instead.
	/// </summary>
	public const byte OnFinishedPort = 5;

	/// <summary>
	/// Output port index for the event emitted when the condition stops holding and ends the loop early, just before
	/// self-deactivation. Fires instead of <see cref="OnFinishedPort"/>, including when the condition already fails
	/// before the first iteration.
	/// </summary>
	public const byte OnConditionFailedPort = 6;
#pragma warning restore RCS1158 // Static member in generic type should use a type parameter

	private enum IterationOutcome
	{
		/// <summary>An iteration ran and the node is still active.</summary>
		Iterated = 0,

		/// <summary>The loop ended on its own; its ending event was emitted and the node deactivated.</summary>
		Ended = 1,

		/// <summary>The iteration deactivated the node or stopped the graph.</summary>
		Interrupted = 2,
	}

	/// <summary>
	/// Declares the node's own parameters: the source input, which takes index 0, and every output variable. The
	/// shared <see cref="ConditionInput"/> and <see cref="IntervalInput"/> declarations are appended afterwards.
	/// </summary>
	/// <param name="inputProperties">The list to add the source input declaration to.</param>
	/// <param name="outputVariables">The list to add output variable declarations to.</param>
	protected abstract void DefineSourceParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables);

	/// <summary>
	/// Reports whether the sequence reaches the given index. Must be free of side effects: it is also called right
	/// after each iteration, to end the loop on the same tick as its last one rather than an interval later.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="index">The zero-based index to test.</param>
	/// <returns><see langword="true"/> if the sequence has an iteration at that index; otherwise,
	/// <see langword="false"/>.</returns>
	protected abstract bool HasIteration(GraphContext graphContext, int index);

	/// <summary>
	/// Prepares the iteration at the given index by writing this node's per-iteration output variables.
	/// </summary>
	/// <remarks>
	/// Called immediately before <see cref="OnIterationPort"/> is emitted, so anything written here is visible to the
	/// nodes that run for that iteration. Only called for an index <see cref="HasIteration"/> accepted.
	/// </remarks>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="index">The zero-based index of the iteration about to run.</param>
	protected abstract void PrepareIteration(GraphContext graphContext, int index);

	/// <summary>
	/// Writes an iteration index to one of this node's output variables, when that variable is bound.
	/// </summary>
	/// <param name="graphContext">The graph's context.</param>
	/// <param name="outputIndex">The index of the output variable declaring the iteration index.</param>
	/// <param name="index">The zero-based index of the iteration about to run.</param>
	protected void WriteIndexOutput(GraphContext graphContext, byte outputIndex, int index)
	{
		OutputVariable output = OutputVariables[outputIndex];

		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetVar(output.BoundName, index);
	}

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnIterationPort, "OnIteration"));
		outputPorts.Add(CreatePort<EventPort>(OnFinishedPort, "OnFinished"));
		outputPorts.Add(CreatePort<EventPort>(OnConditionFailedPort, "OnConditionFailed"));
	}

	/// <inheritdoc/>
	protected sealed override void DefineParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		DefineSourceParameters(inputProperties, outputVariables);

		Validation.Assert(
			inputProperties.Count == 1,
			"An iteration node must declare exactly one source input, so that it occupies index 0.");

		inputProperties.Add(new InputProperty("Condition", typeof(bool)));
		inputProperties.Add(new InputProperty("Interval", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		IterationNodeContext nodeContext = graphContext.GetNodeContext<T>(NodeID);
		nodeContext.NextIndex = 0;
		nodeContext.ElapsedTime = 0;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnActivated(GraphContext graphContext)
	{
		// The first iteration always runs on the activation frame; the interval only spaces out the ones after it. It
		// is resolved after that first iteration so the loop still starts even when a dynamic interval is only
		// meaningful once the loop is under way.
		IterationOutcome outcome = RunNextIteration(graphContext);

		if (outcome != IterationOutcome.Iterated || ResolveInterval(graphContext) > 0)
		{
			return;
		}

		do
		{
			outcome = RunNextIteration(graphContext);
		}
		while (outcome == IterationOutcome.Iterated);
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		double interval = ResolveInterval(graphContext);

		if (interval <= 0)
		{
			// A loop that started synchronously has already deactivated by now, so this only happens when a dynamic
			// interval drops to zero mid-loop. Matching LoopTimerNode, that pauses the node rather than draining it.
			return;
		}

		IterationNodeContext nodeContext = graphContext.GetNodeContext<T>(NodeID);
		nodeContext.ElapsedTime += deltaTime;

		while (nodeContext.ElapsedTime >= interval)
		{
			nodeContext.ElapsedTime -= interval;

			if (RunNextIteration(graphContext) != IterationOutcome.Iterated)
			{
				return;
			}
		}
	}

	private IterationOutcome RunNextIteration(GraphContext graphContext)
	{
		IterationNodeContext nodeContext = graphContext.GetNodeContext<T>(NodeID);

		// The guard is checked before the sequence bound, so a loop whose condition already fails reports that reason
		// rather than reporting an empty sequence it never got to look at.
		if (!ConditionHolds(graphContext))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnConditionFailedPort);
			return IterationOutcome.Ended;
		}

		if (!HasIteration(graphContext, nodeContext.NextIndex))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
			return IterationOutcome.Ended;
		}

		PrepareIteration(graphContext, nodeContext.NextIndex);
		nodeContext.NextIndex++;
		EmitMessage(graphContext, OnIterationPort);

		// An iteration can abort this node or stop the graph, either of which invalidates the loop state this method
		// walks, so the caller must stop rather than run the next one.
		if (!IsNodeActive(graphContext))
		{
			return IterationOutcome.Interrupted;
		}

		// Looking ahead at the sequence bound ends the loop on the same tick as its last iteration, instead of idling
		// a whole interval only to discover there is nothing left. The condition is deliberately not part of this
		// look-ahead: it is a guard evaluated once, when its iteration is due.
		if (!HasIteration(graphContext, nodeContext.NextIndex))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFinishedPort);
			return IterationOutcome.Ended;
		}

		return IterationOutcome.Iterated;
	}

	private bool ConditionHolds(GraphContext graphContext)
	{
		StringKey conditionName = InputProperties[ConditionInput].BoundName;

		return conditionName == StringKey.Empty
			|| (graphContext.TryResolve(conditionName, out bool condition) && condition);
	}

	private double ResolveInterval(GraphContext graphContext)
	{
		return graphContext.TryResolve(InputProperties[IntervalInput].BoundName, out double interval) ? interval : 0;
	}
}
