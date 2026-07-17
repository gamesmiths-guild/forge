// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that listens for level changes on an <see cref="Effect"/> instance while active, emitting an event
/// with the new level.
/// </summary>
/// <remarks>
/// <para>The effect input must resolve to an <see cref="Effect"/>, either a variable-held effect or one bridged from
/// an active-effect handle. Level changes fire when the effect levels up or has its level set (for example by
/// an EffectLevel node), including changes made by other graphs sharing the instance.</para>
/// <para>On each change the node writes the New Level output variable (<see langword="int"/>), then emits
/// <see cref="OnLevelChangedPort"/>. The node stays active until deactivated externally, unsubscribing on
/// deactivation.</para>
/// </remarks>
public class EffectLevelListenerNode : StateNode<EffectLevelListenerNodeContext>
{
	/// <summary>
	/// Input property index for the effect whose level is observed.
	/// </summary>
	public const byte EffectInput = 0;

	/// <summary>
	/// Output variable index for the effect's new level.
	/// </summary>
	public const byte NewLevelOutput = 0;

	/// <summary>
	/// Output port index for the per-change signal.
	/// </summary>
	public const byte OnLevelChangedPort = 4;

	/// <inheritdoc/>
	public override string Description =>
		"Listens for effect level changes while active and emits OnLevelChanged with the new level.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnLevelChangedPort, "OnLevelChanged"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Effect", typeof(Effect)));
		outputVariables.Add(new OutputVariable("New Level", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		EffectLevelListenerNodeContext nodeContext =
			graphContext.GetNodeContext<EffectLevelListenerNodeContext>(NodeID);
		nodeContext.SubscribedEffect = null;
		nodeContext.Handler = null;

		if (!graphContext.TryResolveObject(
			InputProperties[EffectInput].BoundName,
			typeof(Effect),
			out object? resolved)
			|| resolved is not Effect effect)
		{
			return;
		}

		void Handler(int newLevel)
		{
			OnLevelChanged(graphContext, newLevel);
		}

		nodeContext.SubscribedEffect = effect;
		nodeContext.Handler = Handler;

		effect.OnLevelChanged += Handler;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		EffectLevelListenerNodeContext nodeContext =
			graphContext.GetNodeContext<EffectLevelListenerNodeContext>(NodeID);

		if (nodeContext.SubscribedEffect is not null && nodeContext.Handler is not null)
		{
			nodeContext.SubscribedEffect.OnLevelChanged -= nodeContext.Handler;
		}

		nodeContext.SubscribedEffect = null;
		nodeContext.Handler = null;
	}

	private void OnLevelChanged(GraphContext graphContext, int newLevel)
	{
		if (!graphContext.HasNodeContext(NodeID)
			|| !graphContext.GetNodeContext<EffectLevelListenerNodeContext>(NodeID).Active)
		{
			return;
		}

		OutputVariable output = OutputVariables[NewLevelOutput];

		if (output.BoundName != StringKey.Empty)
		{
			Variables? variables = output.Scope == VariableScope.Shared
				? graphContext.SharedVariables
				: graphContext.GraphVariables;

			variables?.SetVar(output.BoundName, newLevel);
		}

		OutputPorts[OnLevelChangedPort].EmitMessage(graphContext);
	}
}
