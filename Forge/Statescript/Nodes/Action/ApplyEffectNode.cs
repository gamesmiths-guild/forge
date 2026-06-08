// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Applies one or more <see cref="Effect"/> instances to one or more entities.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="Effect"/> or an array of effects.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>When either input resolves to an array, the node applies every effect to every target.</para>
/// <para>Because effects are resolved as instances, the same <see cref="Effect"/> can be reused across applications,
/// for example by storing it in a variable and re-reading it. Level and ownership are configured on the resolved effect
/// (see <c>EffectFromDataResolver</c>) rather than on the node.</para>
/// </remarks>
public class ApplyEffectNode : ActionNode
{
	/// <summary>
	/// Input property index for the effect instance.
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Input property index for the effect target.
	/// </summary>
	public const byte TargetInput = 1;

	/// <inheritdoc/>
	public override string Description => "Applies effects to targets.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(Effect)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		EffectApplicationUtilities.ApplyEffects(
			graphContext,
			InputProperties[EffectInput].BoundName,
			InputProperties[TargetInput].BoundName);
	}
}
