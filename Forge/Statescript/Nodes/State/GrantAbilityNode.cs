// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// A state node that grants an ability while active: the ability is granted on activation and the grant is removed
/// on deactivation.
/// </summary>
/// <remarks>
/// <para>The ability-data input must resolve to an <see cref="AbilityData"/>. The entity input selects who receives
/// the grant, defaulting to the ability context's owner when unbound. The level input defaults to the ability
/// context's level, or <c>1</c> without a context. The optional source input records the granting entity.</para>
/// <para>The granted <see cref="AbilityHandle"/> is written to the node's output variable, so the graph can activate
/// or inspect the granted ability, for example through a <see cref="Condition.TryActivateAbilityNode"/>.</para>
/// <para>Grants are reference counted per source: if other grant sources (such as effects) also granted the same
/// ability, removing this node's grant only removes its own share.</para>
/// </remarks>
/// <param name="removalPolicy">How the ability is removed when the node deactivates. Defaults to
/// <see cref="AbilityDeactivationPolicy.CancelImmediately"/>.</param>
/// <param name="levelOverridePolicy">When the ability is already granted, which level relationships override the
/// existing level.</param>
public class GrantAbilityNode(
	AbilityDeactivationPolicy removalPolicy = AbilityDeactivationPolicy.CancelImmediately,
	LevelComparison levelOverridePolicy = LevelComparison.None) : StateNode<GrantAbilityNodeContext>
{
	/// <summary>
	/// Input property index for the ability data to grant.
	/// </summary>
	public const byte AbilityDataInput = 0;

	/// <summary>
	/// Input property index for the entity to grant the ability on.
	/// </summary>
	public const byte EntityInput = 1;

	/// <summary>
	/// Input property index for the ability level.
	/// </summary>
	public const byte LevelInput = 2;

	/// <summary>
	/// Input property index for the optional granting source entity.
	/// </summary>
	public const byte SourceInput = 3;

	/// <summary>
	/// Output variable index for the granted ability handle.
	/// </summary>
	public const byte AbilityOutput = 0;

	private readonly AbilityDeactivationPolicy _removalPolicy = removalPolicy;
	private readonly LevelComparison _levelOverridePolicy = levelOverridePolicy;

	/// <inheritdoc/>
	public override string Description => "Grants an ability while active, removing the grant on deactivation.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability Data", typeof(AbilityData)));
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
		inputProperties.Add(new InputProperty("Source", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Ability", typeof(AbilityHandle)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		GrantAbilityNodeContext nodeContext = graphContext.GetNodeContext<GrantAbilityNodeContext>(NodeID);
		nodeContext.GrantedHandle = null;
		nodeContext.GrantedOn = null;
		nodeContext.GrantSource = null;

		if (!AbilityNodeUtilities.TryResolveAbilityData(
			graphContext,
			InputProperties[AbilityDataInput].BoundName,
			out AbilityData abilityData))
		{
			return;
		}

		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity is null)
		{
			return;
		}

		int level = AbilityNodeUtilities.ResolveLevelOrContext(graphContext, InputProperties[LevelInput].BoundName);
		IForgeEntity? source = AbilityNodeUtilities.ResolveOptionalEntity(
			graphContext,
			InputProperties[SourceInput].BoundName);

		var grantSource = new StatescriptGrantSource(_removalPolicy, AbilityDeactivationPolicy.Ignore);

		AbilityHandle handle = entity.Abilities.GrantAbility(
			abilityData,
			level,
			_levelOverridePolicy,
			grantSource,
			source);

		nodeContext.GrantedHandle = handle;
		nodeContext.GrantedOn = entity;
		nodeContext.GrantSource = grantSource;

		AbilityNodeUtilities.WriteHandleOutput(graphContext, OutputVariables[AbilityOutput], handle);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		GrantAbilityNodeContext nodeContext = graphContext.GetNodeContext<GrantAbilityNodeContext>(NodeID);

		if (nodeContext.GrantedHandle is not null
			&& nodeContext.GrantedOn is not null
			&& nodeContext.GrantSource is not null)
		{
			nodeContext.GrantedOn.Abilities.RemoveGrantedAbility(nodeContext.GrantedHandle, nodeContext.GrantSource);
		}

		nodeContext.GrantedHandle = null;
		nodeContext.GrantedOn = null;
		nodeContext.GrantSource = null;
	}
}
