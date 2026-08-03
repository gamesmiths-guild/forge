// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that emits an iteration event a bounded number of times, optionally guarded by a condition and
/// optionally spaced by an interval, then deactivates.
/// </summary>
/// <remarks>
/// <para>The count input must resolve to an <see langword="int"/> and is re-resolved before every iteration, so a
/// count driven by an attribute can grow or shrink while the loop runs. When it is unbound or non-positive the node
/// runs no iterations at all and finishes immediately — an unbound count is never an endless loop, which matters
/// because the whole loop can run within a single frame.</para>
/// <para>See <see cref="IterationNode{T}"/> for the condition, interval and ending semantics shared with
/// <see cref="ForEachNode"/>.</para>
/// </remarks>
public class RepeatNode : IterationNode<IterationNodeContext>
{
	/// <summary>
	/// Input property index for the number of iterations to run.
	/// </summary>
	public const byte CountInput = 0;

	/// <summary>
	/// Output variable index for the current iteration index.
	/// </summary>
	public const byte IndexOutput = 0;

	/// <inheritdoc/>
	public override string Description => "Emits an iteration event a fixed number of times, all within the " +
		"activation frame or spaced by an interval, then deactivates.";

	/// <inheritdoc/>
	protected override void DefineSourceParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Count", typeof(int)));

		outputVariables.Add(new OutputVariable("Index", typeof(int)));
	}

	/// <inheritdoc/>
	protected override bool HasIteration(GraphContext graphContext, int index)
	{
		return graphContext.TryResolve(InputProperties[CountInput].BoundName, out int count) && index < count;
	}

	/// <inheritdoc/>
	protected override void PrepareIteration(GraphContext graphContext, int index)
	{
		WriteIndexOutput(graphContext, IndexOutput, index);
	}
}
