// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript.Nodes.Condition;

/// <summary>
/// Grants an ability transiently, activates it once, and routes to the True port when the activation succeeds.
/// </summary>
/// <remarks>
/// <para>The granted ability is automatically removed when it ends, the one-shot "proc" pattern.</para>
/// <para>The ability-data input must resolve to an <see cref="AbilityData"/>. The entity input selects who receives
/// the grant, defaulting to the ability context's owner when unbound. The level input defaults to the ability
/// context's level, or <c>1</c> without a context. The optional target input is passed as the activation target.
/// </para>
/// <para>The optional activation-data input supplies an <see cref="AbilityActivator"/> (typically built by an
/// <see cref="Providers.IAbilityActivationDataProvider"/>) carrying strongly-typed data for the activation. Since the
/// ability being activated is known up front, the provider's data type can be matched to it. When the input is unbound,
/// the ability is activated without custom data.</para>
/// </remarks>
/// <param name="levelOverridePolicy">When the ability is already granted, which level relationships override the
/// existing level.</param>
public class GrantAbilityAndActivateOnceNode(LevelComparison levelOverridePolicy = LevelComparison.None)
	: ConditionNode
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
	/// Input property index for the optional activation target.
	/// </summary>
	public const byte TargetInput = 3;

	/// <summary>
	/// Input property index for the optional custom activation data.
	/// </summary>
	public const byte ActivationDataInput = 4;

	private readonly LevelComparison _levelOverridePolicy = levelOverridePolicy;

	/// <inheritdoc/>
	public override string Description =>
		"Grants an ability transiently and activates it once; True when the activation succeeds.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Ability Data", typeof(AbilityData)));
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Level", typeof(int)));
		inputProperties.Add(new InputProperty("Target", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Activation Data", typeof(AbilityActivator), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		if (!AbilityNodeUtilities.TryResolveAbilityData(
			graphContext,
			InputProperties[AbilityDataInput].BoundName,
			out AbilityData abilityData))
		{
			return false;
		}

		IForgeEntity? entity = AbilityNodeUtilities.ResolveEntityOrOwner(
			graphContext,
			InputProperties[EntityInput].BoundName);

		if (entity is null)
		{
			return false;
		}

		int level = AbilityNodeUtilities.ResolveLevelOrContext(graphContext, InputProperties[LevelInput].BoundName);
		IForgeEntity? target = AbilityNodeUtilities.ResolveOptionalEntity(
			graphContext,
			InputProperties[TargetInput].BoundName);

		AbilityActivator? activator = AbilityNodeUtilities.ResolveActivator(
			graphContext,
			InputProperties[ActivationDataInput].BoundName);

		if (activator is not null)
		{
			return activator.GrantAndActivateOnce(
				entity.Abilities,
				abilityData,
				level,
				_levelOverridePolicy,
				target,
				source: null,
				graphContext);
		}

		entity.Abilities.GrantAbilityAndActivateOnce(
			abilityData,
			level,
			_levelOverridePolicy,
			out AbilityActivationFailures failures,
			target);

		// The returned handle is already freed when the ability completed (and was auto-removed) synchronously, so
		// activation success is judged by the failure flags instead.
		return failures == AbilityActivationFailures.None;
	}
}
