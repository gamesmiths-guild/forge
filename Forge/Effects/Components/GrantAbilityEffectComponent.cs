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

	private bool _isInhibited;

	/// <inheritdoc/>
	public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		GrantAbilitiesPermanently(target, effectEvaluatedData);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		GrantAbilities(target, activeEffectEvaluatedData);

		return true;
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		if (removed)
		{
			RemoveGrantedAbilities(target, activeEffectEvaluatedData.ActiveEffectHandle);
		}
	}

	/// <inheritdoc/>
	public void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (_isInhibited != activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited)
		{
			_isInhibited = activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited;
			InhibitGrantedAbilities(target, _isInhibited, activeEffectEvaluatedData.ActiveEffectHandle);
		}
	}

	private void GrantAbilitiesPermanently(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			GrantAbilityConfig config = _grantAbilityConfigs[i];

			target.Abilities.GrantAbilityPermanently(
				config.AbilityData,
				config.ScalableLevel.GetValue(effectEvaluatedData.Level),
				config.RemovalPolicy,
				config.InhibitionPolicy,
				config.LevelOverridePolicy,
				effectEvaluatedData.Effect.Ownership.Owner);
		}
	}

	private void GrantAbilities(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			GrantAbilityConfig config = _grantAbilityConfigs[i];

			_grantedAbilities[i] = target.Abilities.GrantAbility(
				config.AbilityData,
				config.ScalableLevel.GetValue(activeEffectEvaluatedData.EffectEvaluatedData.Level),
				config.RemovalPolicy,
				config.InhibitionPolicy,
				config.LevelOverridePolicy,
				activeEffectEvaluatedData.ActiveEffectHandle,
				activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership.Owner);
		}
	}

	private void RemoveGrantedAbilities(IForgeEntity target, ActiveEffectHandle activeEffectHandle)
	{
		foreach (AbilityHandle ability in _grantedAbilities)
		{
			target.Abilities.RemoveGrantedAbility(ability, activeEffectHandle);
		}
	}

	private void InhibitGrantedAbilities(IForgeEntity target, bool inhibit, ActiveEffectHandle effectHandle)
	{
		foreach (AbilityHandle ability in _grantedAbilities)
		{
			target.Abilities.InhibitGrantedAbility(ability, inhibit, effectHandle);
		}
	}
}
