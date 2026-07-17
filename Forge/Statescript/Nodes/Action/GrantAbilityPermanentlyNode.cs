// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Permanently grants an ability to an entity.
/// </summary>
/// <remarks>
/// <para>Permanent grants cannot be revoked or inhibited, use them for unlock-style progression. For grants tied to
/// a graph state's lifetime, use the GrantAbility state node instead; for data-driven grants, use effects with a
/// grant-ability component.</para>
/// <para>The ability-data input must resolve to an <see cref="AbilityData"/>. The target input defaults to the
/// ability context's owner when unbound. The level input defaults to the ability context's level, or <c>1</c>
/// without a context.</para>
/// <para>The granted <see cref="AbilityHandle"/> is written to the node's output variable.</para>
/// </remarks>
/// <param name="levelOverridePolicy">When the ability is already granted, which level relationships override the
/// existing level.</param>
public class GrantAbilityPermanentlyNode(LevelComparison levelOverridePolicy = LevelComparison.None) : ActionNode
{
	/// <summary>
	/// Input property index for the ability data to grant.
	/// </summary>
	public const byte AbilityDataInput = 0;

	/// <summary>
	/// Input property index for the entity to grant the ability on.
	/// </summary>
	public const byte TargetInput = 1;

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

	private readonly LevelComparison _levelOverridePolicy = levelOverridePolicy;

	/// <inheritdoc/>
	public override string Description => "Permanently grants an ability to an entity (cannot be revoked).";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability Data", typeof(AbilityData)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
		inputProperties.Add(new InputProperty("Source", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Ability", typeof(AbilityHandle)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!AbilityNodeUtilities.TryResolveAbilityData(
			graphContext,
			InputProperties[AbilityDataInput].BoundName,
			out AbilityData abilityData))
		{
			return;
		}

		IForgeEntity? target = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[TargetInput].BoundName);

		if (target is null)
		{
			return;
		}

		int level = AbilityNodeUtilities.ResolveLevelOrContext(graphContext, InputProperties[LevelInput].BoundName);
		IForgeEntity? source = AbilityNodeUtilities.ResolveOptionalEntity(
			graphContext,
			InputProperties[SourceInput].BoundName);

		AbilityHandle handle = target.Abilities.GrantAbilityPermanently(
			abilityData,
			level,
			_levelOverridePolicy,
			source);

		AbilityNodeUtilities.WriteHandleOutput(graphContext, OutputVariables[AbilityOutput], handle);
	}
}
