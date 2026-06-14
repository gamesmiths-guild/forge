// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Applies one or more persistent cues (<see cref="Cues.CuesManager.ApplyCue"/>) on activation and removes them
/// (<see cref="Cues.CuesManager.RemoveCue"/>) on deactivation.
/// </summary>
/// <remarks>
/// <para>The cue-tag input accepts either a single <see cref="Tag"/> or an array of tags; the target input accepts a
/// single <see cref="IForgeEntity"/> or an array. The node applies every cue tag on every target, firing through each
/// target's <see cref="IForgeEntity.CuesManager"/>.</para>
/// <para>The node has no timer: it stays active until it is deactivated externally (typically as a subgraph of another
/// state node). The exact cue/target pairs applied on activation are removed on deactivation.</para>
/// <para>Whether the removal counts as an interruption is derived from how the node was deactivated: a natural shutdown
/// (parent subgraph ending or graph stop) passes <c>interrupted: false</c>, while a deactivation forced through the
/// <c>Abort</c> port passes <c>interrupted: true</c>.</para>
/// </remarks>
public class CueNode : StateNode<CueNodeContext>
{
	/// <summary>
	/// Input property index for the cue tag(s).
	/// </summary>
	public const byte CueTagInput = 0;

	/// <summary>
	/// Input property index for the cue target(s).
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the optional cue magnitude.
	/// </summary>
	public const byte MagnitudeInput = 2;

	/// <summary>
	/// Input property index for the optional normalized cue magnitude.
	/// </summary>
	public const byte NormalizedMagnitudeInput = 3;

	/// <summary>
	/// Input property index for the optional cue source entity.
	/// </summary>
	public const byte SourceInput = 4;

	/// <summary>
	/// Input property index for the optional cue custom parameters.
	/// </summary>
	public const byte CustomParametersInput = 5;

	/// <inheritdoc/>
	public override string Description =>
		"Applies cues while active and removes them on deactivation.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Cue Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Magnitude", typeof(int)));
		inputProperties.Add(new InputProperty("Normalized Magnitude", typeof(float)));
		inputProperties.Add(new InputProperty("Source", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Custom Parameters", typeof(Dictionary<StringKey, object>)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		CueNodeContext nodeContext = graphContext.GetNodeContext<CueNodeContext>(NodeID);
		nodeContext.AppliedCues.Clear();

		CueApplicationUtilities.ApplyCues(
			graphContext,
			InputProperties[CueTagInput].BoundName,
			InputProperties[TargetInput].BoundName,
			InputProperties[MagnitudeInput].BoundName,
			InputProperties[NormalizedMagnitudeInput].BoundName,
			InputProperties[SourceInput].BoundName,
			InputProperties[CustomParametersInput].BoundName,
			nodeContext.AppliedCues);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		CueNodeContext nodeContext = graphContext.GetNodeContext<CueNodeContext>(NodeID);

		// Natural shutdown (subgraph end / graph stop) is not an interruption; an Abort-port deactivation is.
		CueApplicationUtilities.RemoveCues(nodeContext.AppliedCues, nodeContext.WasAborted);
	}
}
