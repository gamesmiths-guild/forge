// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Updates one or more already-active cues (<see cref="Cues.CuesManager.UpdateCue"/>) on one or more targets.
/// </summary>
/// <remarks>
/// <para>The cue-tag input accepts either a single <see cref="Tag"/> or an array of tags.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>When either input resolves to an array, the node updates every cue tag on every target.</para>
/// <para>Magnitude, normalized magnitude, and source are optional; when all are unbound, the cue is updated without
/// parameters. The update is fired through each target's <see cref="IForgeEntity.CuesManager"/>. This is intended for
/// cues applied by a <c>CueNode</c> that need new values without re-applying.</para>
/// </remarks>
public class UpdateCueNode : ActionNode
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
	public override string Description => "Updates active cues on targets.";

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
		CueApplicationUtilities.UpdateCues(
			graphContext,
			InputProperties[CueTagInput].BoundName,
			InputProperties[TargetInput].BoundName,
			InputProperties[MagnitudeInput].BoundName,
			InputProperties[NormalizedMagnitudeInput].BoundName,
			InputProperties[SourceInput].BoundName);
	}
}
