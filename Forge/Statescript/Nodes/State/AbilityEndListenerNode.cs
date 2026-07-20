// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that listens for abilities ending on an entity while active, emitting an event with the ended
/// ability and whether it was canceled.
/// </summary>
/// <remarks>
/// <para>The entity input selects whose abilities are observed, defaulting to the ability context's owner when
/// unbound. When the optional ability-data input is bound, only that granted ability's ends are reported.</para>
/// <para>On each end the node writes the Ability output variable (<see cref="AbilityHandle"/>) and the Was Canceled
/// output (<see langword="bool"/>), then emits <see cref="OnAbilityEndedPort"/>. The node stays active until
/// deactivated externally, unsubscribing on deactivation.</para>
/// <para>When the grant is removed on end (e.g. a RemoveOnEnd policy), the emitted handle may already be freed
/// (<see cref="AbilityHandle.IsValid"/> is <see langword="false"/>); the end is still reported, but the handle is
/// then only useful for identity/logging, not for further queries.</para>
/// </remarks>
public class AbilityEndListenerNode : StateNode<AbilityEndListenerNodeContext>
{
	/// <summary>
	/// Input property index for the entity whose abilities are observed.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the optional ability data filter.
	/// </summary>
	public const byte AbilityDataInput = 1;

	/// <summary>
	/// Output variable index for the ended ability's handle.
	/// </summary>
	public const byte AbilityOutput = 0;

	/// <summary>
	/// Output variable index for whether the ability was canceled.
	/// </summary>
	public const byte WasCanceledOutput = 1;

	/// <summary>
	/// Output port index for the per-end signal.
	/// </summary>
	public const byte OnAbilityEndedPort = 4;

	/// <inheritdoc/>
	public override string Description =>
		"Listens for abilities ending while active and emits OnAbilityEnded with the ability and cancel state.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnAbilityEndedPort, "OnAbilityEnded"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Ability Data", typeof(AbilityData)));
		outputVariables.Add(new OutputVariable("Ability", typeof(AbilityHandle)));
		outputVariables.Add(new OutputVariable("Was Canceled", typeof(bool)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		AbilityEndListenerNodeContext nodeContext =
			graphContext.GetNodeContext<AbilityEndListenerNodeContext>(NodeID);
		nodeContext.SubscribedEntity = null;
		nodeContext.FilterData = null;
		nodeContext.Handler = null;

		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity is null)
		{
			return;
		}

		if (AbilityNodeUtilities.TryResolveAbilityData(
			graphContext,
			InputProperties[AbilityDataInput].BoundName,
			out AbilityData filterData))
		{
			nodeContext.FilterData = filterData;
		}

		void Handler(AbilityEndedData endedData)
		{
			OnAbilityEnded(graphContext, endedData);
		}

		nodeContext.SubscribedEntity = entity;
		nodeContext.Handler = Handler;

		entity.Abilities.OnAbilityEnded += Handler;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		AbilityEndListenerNodeContext nodeContext =
			graphContext.GetNodeContext<AbilityEndListenerNodeContext>(NodeID);

		if (nodeContext.SubscribedEntity is not null && nodeContext.Handler is not null)
		{
			nodeContext.SubscribedEntity.Abilities.OnAbilityEnded -= nodeContext.Handler;
		}

		nodeContext.SubscribedEntity = null;
		nodeContext.FilterData = null;
		nodeContext.Handler = null;
	}

	private static void WriteBoolOutput(GraphContext graphContext, OutputVariable output, bool value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		variables?.SetVar(output.BoundName, value);
	}

	private void OnAbilityEnded(GraphContext graphContext, AbilityEndedData endedData)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		AbilityEndListenerNodeContext nodeContext =
			graphContext.GetNodeContext<AbilityEndListenerNodeContext>(NodeID);

		if (!nodeContext.Active)
		{
			return;
		}

		if (nodeContext.FilterData is AbilityData filterData
			&& endedData.AbilityData != filterData)
		{
			return;
		}

		AbilityNodeUtilities.WriteHandleOutput(graphContext, OutputVariables[AbilityOutput], endedData.Ability);
		WriteBoolOutput(graphContext, OutputVariables[WasCanceledOutput], endedData.WasCanceled);

		OutputPorts[OnAbilityEndedPort].EmitMessage(graphContext);
	}
}
