// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Grant an ability to the target when the effect is applied.
/// </summary>
/// <param name="grantAbilityConfigs">Configurations for the abilities to be granted.</param>
public class GrantAbilityEffectComponent(GrantAbilityConfig[] grantAbilityConfigs) : IEffectComponent
{
	private readonly GrantAbilityConfig[] _grantAbilityConfigs = grantAbilityConfigs;

	private readonly AbilityHandle[] _grantedAbilities = new AbilityHandle[grantAbilityConfigs.Length];

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		GrantAbilities(target, effectEvaluatedData);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		if (removed)
		{
			RemoveGrantedAbilities(target);
		}
	}

	/// <inheritdoc/>
	public void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited)
		{
			RemoveGrantedAbilities(target);
		}
		else
		{
			GrantAbilities(target, activeEffectEvaluatedData.EffectEvaluatedData);
		}
	}

	private void GrantAbilities(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			GrantAbilityConfig config = _grantAbilityConfigs[i];

			_grantedAbilities[i] = target.Abilities.GrantAbility(
				config.AbilityData,
				config.ScalableLevel.GetValue(effectEvaluatedData.Level),
				config.RemovalPolicy,
				config.LevelOverridePolicy,
				effectEvaluatedData.Effect.Ownership.Owner);
		}
	}

	private void RemoveGrantedAbilities(IForgeEntity target)
	{
		foreach (AbilityHandle ability in _grantedAbilities)
		{
			target.Abilities.RemoveGrantedAbility(ability);
		}
	}
}
