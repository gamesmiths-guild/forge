// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Stacking;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Manages the <see cref="Effect"/> application and instances of an entity.
/// </summary>
/// <param name="owner">The owner of this manager.</param>
/// <param name="cuesManager">The cues manager to be used to trigger cues by this effects manager.</param>
public class EffectsManager(IForgeEntity owner, CuesManager cuesManager)
{
	private readonly CuesManager _cuesManager = cuesManager;

	private readonly List<ActiveEffect> _activeEffects = [];

	/// <summary>
	/// Gets the owner of this effects manager.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Applies an effect to the owner of this manager.
	/// </summary>
	/// <param name="effect">The instance of the effect to be applied.</param>
	/// <returns>A handle to the applied effect if it was successfully applied as an <see cref="ActiveEffect"/>.
	/// </returns>
	public ActiveEffectHandle? ApplyEffect(Effect effect)
	{
		if (!effect.CanApply(Owner))
		{
			return null;
		}

		if (effect.EffectData.DurationData.DurationType == DurationType.Instant)
		{
			var evaluatedData = new EffectEvaluatedData(effect, Owner);

			foreach (IEffectComponent component in effect.EffectData.EffectComponents)
			{
				component.OnEffectApplied(Owner, in evaluatedData);
			}

			Effect.Execute(in evaluatedData);
			return null;
		}

		if (!effect.EffectData.StackingData.HasValue)
		{
			return ApplyNewEffect(effect).Handle;
		}

		ActiveEffect? stackableEffect = FindStackableEffect(effect);

		if (stackableEffect is not null)
		{
			var successfulApplication = stackableEffect.AddStack(effect);

			if (successfulApplication)
			{
				foreach (IEffectComponent component in stackableEffect.EffectData.EffectComponents)
				{
					component.OnEffectApplied(Owner, stackableEffect.EffectEvaluatedData);
				}
			}

			return stackableEffect.Handle;
		}

		return ApplyNewEffect(effect).Handle;
	}

	/// <summary>
	/// Removes an application of an <see cref="ActiveEffect"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <param name="activeEffect">The instance of the active effect to be removed.</param>
	/// <param name="forceUnapply">Forces unapplication even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void UnapplyEffect(ActiveEffectHandle activeEffect, bool forceUnapply = false)
	{
		RemoveStackOrUnapply(activeEffect.ActiveEffect, forceUnapply);
	}

	/// <summary>
	/// Removes an application of a <see cref="Effect"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <param name="effect">The instance of the effect to be removed.</param>
	/// <param name="forceUnapply">Forces unapplication even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void UnapplyEffect(Effect effect, bool forceUnapply = false)
	{
		RemoveStackOrUnapply(FilterEffectsByEffect(effect).FirstOrDefault(), forceUnapply);
	}

	/// <summary>
	/// Unapply an effect based on an <see cref="EffectData"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <remarks>
	/// This method searches for the first instance of the given effect data it can find and removes it.
	/// </remarks>
	/// <param name="effectData">Which effect data to look for to removal.</param>
	/// /// <param name="forceUnapply">Forces unapplication even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void UnapplyEffectData(EffectData effectData, bool forceUnapply = false)
	{
		RemoveStackOrUnapply(FilterEffectsByData(effectData).FirstOrDefault(), forceUnapply);
	}

	/// <summary>
	/// Updates effects and their time.
	/// </summary>
	/// <remarks>
	/// This could be hook up into the engine's time or controlled by your game's logic updating only when turns passes.
	/// </remarks>
	/// <param name="deltaTime">Time passed since the last update call.</param>
	public void UpdateEffects(double deltaTime)
	{
		foreach (ActiveEffect effect in _activeEffects)
		{
			effect.Update(deltaTime);
		}

		foreach (ActiveEffect expiredEffect in _activeEffects.Where(x => x.IsExpired).ToArray())
		{
			RemoveActiveEffect(expiredEffect, false);
		}
	}

	/// <summary>
	/// Queries and gets information about application of a given <see cref="EffectData"/>.
	/// </summary>
	/// <param name="effectData">Which effect to query for.</param>
	/// <returns>A list of <see cref="EffectStackInstanceData"/> grouped by their stack configuration.</returns>
	public IEnumerable<EffectStackInstanceData> GetEffectInfo(EffectData effectData)
	{
		IEnumerable<ActiveEffect> filteredEffects = FilterEffectsByData(effectData);
		return ConvertToStackInstanceData(filteredEffects);
	}

	internal void OnEffectExecuted_InternalCall(EffectEvaluatedData executedEffectEvaluatedData)
	{
		EffectData effectData = executedEffectEvaluatedData.Effect.EffectData;

		foreach (IEffectComponent component in effectData.EffectComponents)
		{
			component.OnEffectExecuted(Owner, in executedEffectEvaluatedData);
		}

		_cuesManager.ExecuteCues(in executedEffectEvaluatedData);
	}

	internal void OnActiveEffectUnapplied_InternalCall(ActiveEffect removedEffect)
	{
		foreach (IEffectComponent component in removedEffect.Effect.EffectData.EffectComponents)
		{
			component.OnActiveEffectUnapplied(
				Owner,
				new ActiveEffectEvaluatedData(
					removedEffect.Handle,
					removedEffect.EffectEvaluatedData,
					removedEffect.RemainingDuration,
					removedEffect.NextPeriodicTick,
					removedEffect.ExecutionCount),
				false);
		}
	}

	internal void OnActiveEffectChanged_InternalCall(ActiveEffect removedEffect)
	{
		foreach (IEffectComponent component in removedEffect.EffectData.EffectComponents)
		{
			component.OnActiveEffectChanged(
				Owner,
				new ActiveEffectEvaluatedData(
					removedEffect.Handle,
					removedEffect.EffectEvaluatedData,
					removedEffect.RemainingDuration,
					removedEffect.NextPeriodicTick,
					removedEffect.ExecutionCount));
		}
	}

	internal void TriggerCuesUpdate_InternalCall(in EffectEvaluatedData effectEvaluatedData)
	{
		_cuesManager.UpdateCues(in effectEvaluatedData);
	}

	private static bool MatchesStackPolicy(ActiveEffect existingEffect, Effect newEffect)
	{
		Validation.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget ||
			   existingEffect.EffectEvaluatedData.Effect.Ownership.Owner == newEffect.Ownership.Owner;
	}

	private static bool MatchesStackLevelPolicy(ActiveEffect existingEffect, Effect newEffect)
	{
		Validation.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels ||
			   existingEffect.EffectEvaluatedData.Effect.Level == newEffect.Level;
	}

	private static IEnumerable<EffectStackInstanceData> ConvertToStackInstanceData(
		IEnumerable<ActiveEffect> filteredEffects)
	{
		return filteredEffects.Select(CreateStackInstanceData);
	}

	private static EffectStackInstanceData CreateStackInstanceData(ActiveEffect effect)
	{
		EffectEvaluatedData evaluatedData = effect.EffectEvaluatedData;
		return new EffectStackInstanceData(
			evaluatedData.Effect.Ownership.Owner,
			evaluatedData.Level,
			evaluatedData.Stack);
	}

	private IEnumerable<ActiveEffect> FilterEffectsByData(EffectData effectData)
	{
		return _activeEffects.Where(x => x.EffectEvaluatedData.Effect.EffectData == effectData);
	}

	private IEnumerable<ActiveEffect> FilterEffectsByEffect(Effect effect)
	{
		return _activeEffects.Where(x => x.EffectEvaluatedData.Effect == effect);
	}

	private ActiveEffect? FindStackableEffect(Effect effect)
	{
		return FilterEffectsByData(effect.EffectData).FirstOrDefault(x =>
			MatchesStackPolicy(x, effect) &&
			MatchesStackLevelPolicy(x, effect));
	}

	private ActiveEffect ApplyNewEffect(Effect effect)
	{
		var activeEffect = new ActiveEffect(effect, Owner);
		_activeEffects.Add(activeEffect);

		var remainActive = true;

		foreach (IEffectComponent component in effect.EffectData.EffectComponents)
		{
			remainActive &= component.OnActiveEffectAdded(
				Owner,
				new ActiveEffectEvaluatedData(
					activeEffect.Handle,
					activeEffect.EffectEvaluatedData,
					activeEffect.RemainingDuration,
					activeEffect.NextPeriodicTick,
					activeEffect.ExecutionCount));
			component.OnEffectApplied(Owner, activeEffect.EffectEvaluatedData);
		}

		EffectEvaluatedData effectEvaluatedData = activeEffect.EffectEvaluatedData;

		var triggerApplyCuesEarly = effect.EffectData.PeriodicData.HasValue
			&& effect.EffectData.PeriodicData.Value.ExecuteOnApplication
			&& remainActive;

		if (triggerApplyCuesEarly)
		{
			_cuesManager.ApplyCues(in effectEvaluatedData);
		}

		activeEffect.Apply(inhibited: !remainActive);

		if (!triggerApplyCuesEarly)
		{
			_cuesManager.ApplyCues(in effectEvaluatedData);
		}

		effectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();

		foreach (IEffectComponent component in effect.EffectData.EffectComponents)
		{
			component.OnPostActiveEffectAdded(
				Owner,
				new ActiveEffectEvaluatedData(
					activeEffect.Handle,
					activeEffect.EffectEvaluatedData,
					activeEffect.RemainingDuration,
					activeEffect.NextPeriodicTick,
					activeEffect.ExecutionCount));
		}

		return activeEffect;
	}

	private void RemoveStackOrUnapply(ActiveEffect? effectToRemove, bool forceUnapply)
	{
		if (effectToRemove is null)
		{
			return;
		}

		if (!forceUnapply
			&& effectToRemove.EffectData.StackingData.HasValue
			&& effectToRemove.EffectData.StackingData.Value.ExpirationPolicy
			== StackExpirationPolicy.RemoveSingleStackAndRefreshDuration)
		{
			effectToRemove.RemoveStack();
			effectToRemove.RemainingDuration = effectToRemove.EffectEvaluatedData.Duration;

			if (effectToRemove.StackCount == 0)
			{
				RemoveActiveEffect(effectToRemove, false);
			}

			return;
		}

		if (effectToRemove.EffectData.DurationData.DurationType == DurationType.HasDuration)
		{
			forceUnapply = true;
		}

		effectToRemove.Unapply();
		RemoveActiveEffect(effectToRemove, forceUnapply);
	}

	private void RemoveActiveEffect(ActiveEffect effectToRemove, bool interrupted)
	{
		if (!_activeEffects.Contains(effectToRemove))
		{
			return;
		}

		_activeEffects.Remove(effectToRemove);

		EffectEvaluatedData effectEvaluatedData = effectToRemove.EffectEvaluatedData;

		foreach (IEffectComponent component in effectToRemove.EffectData.EffectComponents)
		{
			component.OnActiveEffectUnapplied(
				Owner,
				new ActiveEffectEvaluatedData(
					effectToRemove.Handle,
					effectEvaluatedData,
					effectToRemove.RemainingDuration,
					effectToRemove.NextPeriodicTick,
					effectToRemove.ExecutionCount),
				true);
		}

		effectToRemove.Handle.Free();

		effectToRemove.EffectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();

		_cuesManager.RemoveCues(in effectEvaluatedData, interrupted);
	}
}
