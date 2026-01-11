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
	private readonly IAbilityGrantSource[] _grantSources = new IAbilityGrantSource[grantAbilityConfigs.Length];

	private bool _hasGrantedAbilities;
	private bool _isInhibited;

	/// <summary>
	/// Gets a read-only list of the granted abilities.
	/// </summary>
	public IReadOnlyList<AbilityHandle> GrantedAbilities => _grantedAbilities;

	/// <inheritdoc/>
	public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (_hasGrantedAbilities)
		{
			return;
		}

		GrantAbilitiesPermanently(target, effectEvaluatedData);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		GrantAbilities(target, activeEffectEvaluatedData);

		return true;
	}

	/// <inheritdoc/>
	public void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited)
		{
			_isInhibited = activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited;
			InhibitGrantedAbilities(target);
			return;
		}

		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			if (_grantAbilityConfigs[i].TryActivateOnGrant)
			{
				_grantedAbilities[i].Activate(out _);
			}
		}
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
		if (_isInhibited != activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited)
		{
			_isInhibited = activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited;
			InhibitGrantedAbilities(target);

			if (!_isInhibited)
			{
				for (var i = 0; i < _grantAbilityConfigs.Length; i++)
				{
					if (_grantAbilityConfigs[i].TryActivateOnEnable)
					{
						_grantedAbilities[i].Activate(out _);
					}
				}
			}
		}
	}

	private void GrantAbilitiesPermanently(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			GrantAbilityConfig config = _grantAbilityConfigs[i];

			_grantedAbilities[i] = target.Abilities.GrantAbilityPermanently(
				config.AbilityData,
				config.ScalableLevel.GetValue(effectEvaluatedData.Level),
				config.LevelOverridePolicy,
				effectEvaluatedData.Effect.Ownership.Source);
		}

		_hasGrantedAbilities = true;

		// Try to activate on grant for permanent abilities
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			if (_grantAbilityConfigs[i].TryActivateOnGrant)
			{
				_grantedAbilities[i].Activate(out _);
			}
		}
	}

	private void GrantAbilities(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		for (var i = 0; i < _grantAbilityConfigs.Length; i++)
		{
			GrantAbilityConfig config = _grantAbilityConfigs[i];

			var grantSource = new EffectGrantSource(
				activeEffectEvaluatedData.ActiveEffectHandle,
				config.RemovalPolicy,
				config.InhibitionPolicy);
			_grantSources[i] = grantSource;

			_grantedAbilities[i] = target.Abilities.GrantAbility(
				config.AbilityData,
				config.ScalableLevel.GetValue(activeEffectEvaluatedData.EffectEvaluatedData.Level),
				config.LevelOverridePolicy,
				grantSource,
				activeEffectEvaluatedData.EffectEvaluatedData.Effect.Ownership.Source);
		}

		_hasGrantedAbilities = true;
	}

	private void RemoveGrantedAbilities(IForgeEntity target)
	{
		for (var i = 0; i < _grantedAbilities.Length; i++)
		{
			AbilityHandle ability = _grantedAbilities[i];
			target.Abilities.RemoveGrantedAbility(ability, _grantSources[i]);
		}
	}

	private void InhibitGrantedAbilities(IForgeEntity target)
	{
		for (var i = 0; i < _grantedAbilities.Length; i++)
		{
			AbilityHandle ability = _grantedAbilities[i];
			target.Abilities.InhibitGrantedAbility(ability, _grantSources[i]);
		}
	}
}
