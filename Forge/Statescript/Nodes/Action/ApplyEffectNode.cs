// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Applies one or more <see cref="EffectData"/> values to one or more entities.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="EffectData"/> or an array of effects.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>When either input resolves to an array, the node applies every effect to every target.</para>
/// </remarks>
public class ApplyEffectNode : ActionNode
{
	/// <summary>
	/// Input property index for the effect data.
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Input property index for the effect target.
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the effect level override.
	/// </summary>
	public const byte LevelInput = 2;

	/// <summary>
	/// Input property index for the effect ownership override.
	/// </summary>
	public const byte OwnershipInput = 3;

	/// <inheritdoc/>
	public override string Description => "Applies effects to targets.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(EffectData)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
		inputProperties.Add(new InputProperty("Ownership", typeof(EffectOwnership)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		EffectApplicationUtilities.ApplyEffects(
			graphContext,
			InputProperties[EffectInput].BoundName,
			InputProperties[TargetInput].BoundName,
			InputProperties[LevelInput].BoundName,
			InputProperties[OwnershipInput].BoundName);
	}
}
