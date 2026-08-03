// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that walks an array, publishing each element to a graph variable and emitting an iteration event for
/// it, optionally guarded by a condition and optionally spaced by an interval.
/// </summary>
/// <remarks>
/// <para>Any array works: a value-typed one (numbers, booleans, tags read through the value lane) or an object-backed
/// one (entities, effects, handles). The <b>bound element variable decides which lane the source is read through</b>,
/// exactly like <see cref="Action.SetVariableNode"/>'s target: an object-backed element variable pulls an array of its
/// own declared element type, a value-typed one pulls a value array. Nothing converts between the lanes, so binding
/// an element variable whose type does not match the source yields no iterations at all rather than a wrong-typed
/// write. Leaving the element variable unbound is allowed — the array is then iterated for its length and index
/// alone.</para>
/// <para>The array is <b>snapshotted on activation</b>, so an iteration that mutates the source variable does not
/// change the sequence being walked, and a loop spread over several frames by the interval input keeps iterating the
/// array it started with.</para>
/// <para>See <see cref="IterationNode{T}"/> for the condition, interval and ending semantics shared with
/// <see cref="RepeatNode"/>.</para>
/// </remarks>
public class ForEachNode : IterationNode<ForEachNodeContext>
{
	/// <summary>
	/// Input property index for the array to iterate.
	/// </summary>
	public const byte ArrayInput = 0;

	/// <summary>
	/// Output variable index for the element of the current iteration.
	/// </summary>
	public const byte ElementOutput = 0;

	/// <summary>
	/// Output variable index for the current iteration index.
	/// </summary>
	public const byte IndexOutput = 1;

	/// <inheritdoc/>
	public override string Description => "Iterates an array, publishing each element to a variable, all within the " +
		"activation frame or spaced by an interval, then deactivates.";

	/// <inheritdoc/>
	protected override void DefineSourceParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Array", typeof(object[])));

		outputVariables.Add(new OutputVariable("Element", typeof(object)));
		outputVariables.Add(new OutputVariable("Index", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		base.OnActivate(graphContext);
		SnapshotSource(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		ForEachNodeContext nodeContext = graphContext.GetNodeContext<ForEachNodeContext>(NodeID);
		nodeContext.Values = null;
		nodeContext.ObjectValues = null;
		nodeContext.ElementVariables = null;
	}

	/// <inheritdoc/>
	protected override bool HasIteration(GraphContext graphContext, int index)
	{
		ForEachNodeContext nodeContext = graphContext.GetNodeContext<ForEachNodeContext>(NodeID);

		int length = nodeContext.ObjectValues?.Length ?? nodeContext.Values?.Length ?? 0;

		return index < length;
	}

	/// <inheritdoc/>
	protected override void PrepareIteration(GraphContext graphContext, int index)
	{
		ForEachNodeContext nodeContext = graphContext.GetNodeContext<ForEachNodeContext>(NodeID);

		if (nodeContext.ObjectValues is { } objectValues)
		{
			nodeContext.ElementVariables?.SetObject(nodeContext.ElementVariableName, objectValues[index]);
		}
		else if (nodeContext.Values is { } values)
		{
			nodeContext.ElementVariables?.SetVariant(nodeContext.ElementVariableName, values[index]);
		}

		WriteIndexOutput(graphContext, IndexOutput, index);
	}

	private static Variables? ResolveElementVariables(GraphContext graphContext, OutputVariable elementOutput)
	{
		if (elementOutput.BoundName == StringKey.Empty)
		{
			return null;
		}

		return elementOutput.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;
	}

	private void SnapshotSource(GraphContext graphContext)
	{
		ForEachNodeContext nodeContext = graphContext.GetNodeContext<ForEachNodeContext>(NodeID);
		nodeContext.Values = null;
		nodeContext.ObjectValues = null;
		nodeContext.ElementVariables = null;

		StringKey sourceName = InputProperties[ArrayInput].BoundName;

		if (sourceName == StringKey.Empty)
		{
			return;
		}

		OutputVariable elementOutput = OutputVariables[ElementOutput];
		Variables? elementVariables = ResolveElementVariables(graphContext, elementOutput);

		if (elementVariables is not null
			&& elementVariables.TryGetObjectVariableType(elementOutput.BoundName, out Type? elementType))
		{
			// An object-backed element variable types the read: a source that is not an array of that type resolves
			// nothing, which reads as an empty sequence rather than silently writing a mismatched element.
			if (graphContext.TryResolveObjectArray(sourceName, elementType, out object?[]? typedValues))
			{
				nodeContext.ObjectValues = typedValues;
				nodeContext.ElementVariables = elementVariables;
				nodeContext.ElementVariableName = elementOutput.BoundName;
			}

			return;
		}

		if (graphContext.TryResolveArray(sourceName, out Variant128[]? values))
		{
			nodeContext.Values = values;

			if (elementVariables?.TryGetVariant(elementOutput.BoundName, out _) == true)
			{
				nodeContext.ElementVariables = elementVariables;
				nodeContext.ElementVariableName = elementOutput.BoundName;
			}

			return;
		}

		// No element variable to type the read, or a value-typed one over an object-backed source: the loop still has
		// an index to offer, so the source is iterated for its length alone.
		if (graphContext.TryResolveObjectArray(sourceName, typeof(object), out object?[]? untypedValues))
		{
			nodeContext.ObjectValues = untypedValues;
		}
	}
}
