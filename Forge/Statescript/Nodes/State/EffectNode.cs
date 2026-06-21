// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Applies one or more effects, stays active while any applied instance is still active, and removes still-active
/// instances on deactivation.
/// </summary>
/// <remarks>
/// <para>The effect input accepts either a single <see cref="Effect"/> or an array of effects.</para>
/// <para>The target input accepts either a single <see cref="IForgeEntity"/> or an array of entities.</para>
/// <para>Instant effects do not produce removable handles, so deactivation only removes effects that are still active.
/// </para>
/// <para>Because effects are resolved as instances, the same <see cref="Effect"/> can be reused across applications.
/// Level and ownership are configured on the resolved effect (see <c>EffectFromDataResolver</c>) rather than on the
/// node.</para>
/// <para>The optional context-data input supplies a custom <see cref="EffectApplicationContext"/> (typically built by
/// an <see cref="Providers.IEffectContextDataProvider"/>) that is passed through the effect pipeline for every
/// application. When the input is unbound, effects are applied without context data.</para>
/// </remarks>
public class EffectNode : StateNode<EffectNodeContext>
{
	/// <summary>
	/// Input property index for the effect instance.
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Input property index for the effect target.
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the optional effect application context data.
	/// </summary>
	public const byte ContextDataInput = 2;

	/// <summary>
	/// Output variable index for the applied effect handle(s).
	/// </summary>
	public const byte ActiveEffectOutput = 0;

	/// <summary>
	/// Output port index for the natural effect-end event.
	/// </summary>
	public const byte OnEffectEndPort = 4;

	/// <inheritdoc/>
	public override string Description =>
		"Applies effects, stays active while any applied effect remains active, emits OnEffectEnd when they all end " +
		"naturally, and removes still-active instances on deactivation.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnEffectEndPort, "OnEffectEnd"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(Effect)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Context Data", typeof(EffectApplicationContext)));
		outputVariables.Add(new OutputVariable("Active Effect", typeof(ActiveEffectHandle)));
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
			nodeContext.ActiveEffectHandles,
			InputProperties[ContextDataInput].BoundName);

		EffectApplicationUtilities.WriteHandleOutput(
			graphContext,
			OutputVariables[ActiveEffectOutput],
			nodeContext.ActiveEffectHandles);

		if (!EffectApplicationUtilities.RetainActiveEffects(nodeContext.ActiveEffectHandles))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnEffectEndPort);
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		EffectNodeContext nodeContext = graphContext.GetNodeContext<EffectNodeContext>(NodeID);
		EffectApplicationUtilities.RemoveEffects(nodeContext.ActiveEffectHandles);
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		EffectNodeContext nodeContext = graphContext.GetNodeContext<EffectNodeContext>(NodeID);

		if (!EffectApplicationUtilities.RetainActiveEffects(nodeContext.ActiveEffectHandles))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnEffectEndPort);
		}
	}
}
