// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Applies one or more effects while the node is active and removes still-active instances on deactivation.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="EffectData"/> or an array of effects.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>Instant effects do not produce removable handles, so deactivation only removes effects that are still active.
/// </para>
/// </remarks>
public class EffectNode : StateNode<EffectNodeContext>
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
	public override string Description =>
		"Applies effects while active and removes still-active instances on deactivation.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(EffectData)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
		inputProperties.Add(new InputProperty("Ownership", typeof(EffectOwnership)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		EffectNodeContext nodeContext = graphContext.GetNodeContext<EffectNodeContext>(NodeID);
		nodeContext.ActiveEffectHandles.Clear();

		EffectApplicationUtilities.ApplyEffects(
			graphContext,
			InputProperties[EffectInput].BoundName,
			InputProperties[TargetInput].BoundName,
			InputProperties[LevelInput].BoundName,
			InputProperties[OwnershipInput].BoundName,
			nodeContext.ActiveEffectHandles);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		EffectNodeContext nodeContext = graphContext.GetNodeContext<EffectNodeContext>(NodeID);
		EffectApplicationUtilities.RemoveEffects(nodeContext.ActiveEffectHandles);
	}
}
