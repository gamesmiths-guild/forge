// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Executes one or more one-shot cues (<see cref="Cues.CuesManager.ExecuteCue"/>) on one or more targets.
/// </summary>
/// <remarks>
/// <para>The cue-tag input accepts either a single <see cref="Tag"/> or an array of tags.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>When either input resolves to an array, the node fires every cue tag on every target.</para>
/// <para>Magnitude, normalized magnitude, and source are optional; when all are unbound, the cue is executed without
/// parameters. The cue is fired through each target's <see cref="IForgeEntity.CuesManager"/>.</para>
/// </remarks>
public class ExecuteCueNode : ActionNode
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

	/// <inheritdoc/>
	public override string Description => "Executes one-shot cues on targets.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Cue Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Magnitude", typeof(int)));
		inputProperties.Add(new InputProperty("Normalized Magnitude", typeof(float)));
		inputProperties.Add(new InputProperty("Source", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		CueApplicationUtilities.ExecuteCues(
			graphContext,
			InputProperties[CueTagInput].BoundName,
			InputProperties[TargetInput].BoundName,
			InputProperties[MagnitudeInput].BoundName,
			InputProperties[NormalizedMagnitudeInput].BoundName,
			InputProperties[SourceInput].BoundName);
	}
}
