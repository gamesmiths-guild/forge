// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Stacking;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Manages the <see cref="GameplayEffect"/> application and instances of an entity.
/// </summary>
/// <param name="owner">The owner of this manager.</param>
public class GameplayEffectsManager(IForgeEntity owner)
{
	private readonly List<ActiveGameplayEffect> _activeEffects = [];

	/// <summary>
	/// Gets the owner of this gameplay effects manager.
	/// </summary>
	public IForgeEntity Owner { get; } = owner;

	/// <summary>
	/// Applies an effect to the owner of this manager.
	/// </summary>
	/// <param name="gameplayEffect">The instance of the gameplay effect to be applied.</param>
	public void ApplyEffect(GameplayEffect gameplayEffect)
	{
		if (!gameplayEffect.CanApply(Owner))
		{
			return;
		}

		if (gameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			var evaluatedData = new GameplayEffectEvaluatedData(gameplayEffect, Owner);

			foreach (IGameplayEffectComponent component in gameplayEffect.EffectData.GameplayEffectComponents)
			{
				component.OnGameplayEffectApplied(Owner, in evaluatedData);
			}

			GameplayEffect.Execute(evaluatedData);
			return;
		}

		if (!gameplayEffect.EffectData.StackingData.HasValue)
		{
			ApplyNewEffect(gameplayEffect);
			return;
		}

		ActiveGameplayEffect? stackableEffect = FindStackableEffect(gameplayEffect);

		if (stackableEffect is not null)
		{
			var successfulApplication = stackableEffect.AddStack(gameplayEffect);

			if (successfulApplication)
			{
				foreach (IGameplayEffectComponent component in stackableEffect.EffectData.GameplayEffectComponents)
				{
					component.OnGameplayEffectApplied(Owner, stackableEffect.GameplayEffectEvaluatedData);
				}
			}

			return;
		}

		ApplyNewEffect(gameplayEffect);
	}

	/// <summary>
	/// Removes an application of a <see cref="GameplayEffect"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <param name="gameplayEffect">The instance of the gameplay effect to be removed.</param>
	/// <param name="forceUnapply">Forces unapplication even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void UnapplyEffect(GameplayEffect gameplayEffect, bool forceUnapply = false)
	{
		RemoveStackOrUnapply(FilterEffectsByGameplayEffect(gameplayEffect).FirstOrDefault(), forceUnapply);
	}

	/// <summary>
	/// Unapply an effect based on an <see cref="GameplayEffectData"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <remarks>
	/// This method searches for the first instance of the given effect data it can find and removes it.
	/// </remarks>
	/// <param name="gameplayEffectData">Which gameplay effect data to look for to removal.</param>
	/// /// <param name="forceUnapply">Forces unapplication even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void UnapplyEffectData(GameplayEffectData gameplayEffectData, bool forceUnapply = false)
	{
		RemoveStackOrUnapply(FilterEffectsByData(gameplayEffectData).FirstOrDefault(), forceUnapply);
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
		foreach (ActiveGameplayEffect effect in _activeEffects)
		{
			effect.Update(deltaTime);

			foreach (IGameplayEffectComponent component in effect.EffectData.GameplayEffectComponents)
			{
				component.OnGameplayEffectUpdated(Owner, new ActiveEffectEvaluatedData(
					effect.GameplayEffectEvaluatedData,
					effect.RemainingDuration,
					effect.NextPeriodicTick,
					effect.ExecutionCount));
			}
		}

		foreach (ActiveGameplayEffect expiredEffect in _activeEffects.Where(x => x.IsExpired).ToArray())
		{
			RemoveActiveGameplayEffect(expiredEffect);
		}
	}

	/// <summary>
	/// Queries and gets information about application of a given <see cref="GameplayEffectData"/>.
	/// </summary>
	/// <param name="effectData">Which gameplay effect to query for.</param>
	/// <returns>A list of <see cref="GameplayEffectStackInstanceData"/> grouped by their stack configuration.</returns>
	public IEnumerable<GameplayEffectStackInstanceData> GetEffectInfo(GameplayEffectData effectData)
	{
		IEnumerable<ActiveGameplayEffect> filteredEffects = FilterEffectsByData(effectData);
		return ConvertToStackInstanceData(filteredEffects);
	}

	internal void OnGameplayEffectExecuted_InternalCall(GameplayEffectEvaluatedData executedEffectEvaluatedData)
	{
		foreach (IGameplayEffectComponent component in
			executedEffectEvaluatedData.GameplayEffect.EffectData.GameplayEffectComponents)
		{
			component.OnGameplayEffectExecuted(Owner, in executedEffectEvaluatedData);
		}
	}

	internal void OnActiveGameplayEffectRemoved_InternalCall(ActiveGameplayEffect removedEffect)
	{
		foreach (IGameplayEffectComponent component in removedEffect.GameplayEffect.EffectData.GameplayEffectComponents)
		{
			component.OnActiveGameplayEffectRemoved(
				Owner,
				new ActiveEffectEvaluatedData(
					removedEffect.GameplayEffectEvaluatedData,
					removedEffect.RemainingDuration,
					removedEffect.NextPeriodicTick,
					removedEffect.ExecutionCount));
		}
	}

	private static bool MatchesStackPolicy(ActiveGameplayEffect existingEffect, GameplayEffect newEffect)
	{
		Debug.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget ||
			   existingEffect.GameplayEffectEvaluatedData.GameplayEffect.Ownership.Owner == newEffect.Ownership.Owner;
	}

	private static bool MatchesStackLevelPolicy(ActiveGameplayEffect existingEffect, GameplayEffect newEffect)
	{
		Debug.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels ||
			   existingEffect.GameplayEffectEvaluatedData.GameplayEffect.Level == newEffect.Level;
	}

	private static IEnumerable<GameplayEffectStackInstanceData> ConvertToStackInstanceData(
		IEnumerable<ActiveGameplayEffect> filteredEffects)
	{
		return filteredEffects.Select(CreateStackInstanceData);
	}

	private static GameplayEffectStackInstanceData CreateStackInstanceData(ActiveGameplayEffect effect)
	{
		GameplayEffectEvaluatedData evaluatedData = effect.GameplayEffectEvaluatedData;
		return new GameplayEffectStackInstanceData(
			evaluatedData.GameplayEffect.Ownership.Owner,
			evaluatedData.Level,
			evaluatedData.Stack);
	}

	private IEnumerable<ActiveGameplayEffect> FilterEffectsByData(GameplayEffectData effectData)
	{
		return _activeEffects.Where(x => x.GameplayEffectEvaluatedData.GameplayEffect.EffectData == effectData);
	}

	private IEnumerable<ActiveGameplayEffect> FilterEffectsByGameplayEffect(GameplayEffect effect)
	{
		return _activeEffects.Where(x => x.GameplayEffectEvaluatedData.GameplayEffect == effect);
	}

	private ActiveGameplayEffect? FindStackableEffect(GameplayEffect gameplayEffect)
	{
		return FilterEffectsByData(gameplayEffect.EffectData).FirstOrDefault(x =>
			MatchesStackPolicy(x, gameplayEffect) &&
			MatchesStackLevelPolicy(x, gameplayEffect));
	}

	private void ApplyNewEffect(GameplayEffect gameplayEffect)
	{
		var activeEffect = new ActiveGameplayEffect(gameplayEffect, Owner);
		_activeEffects.Add(activeEffect);
		activeEffect.Apply();

		foreach (IGameplayEffectComponent component in gameplayEffect.EffectData.GameplayEffectComponents)
		{
			component.OnActiveGameplayEffectAdded(
				Owner,
				new ActiveEffectEvaluatedData(
					activeEffect.GameplayEffectEvaluatedData,
					activeEffect.RemainingDuration,
					activeEffect.NextPeriodicTick,
					activeEffect.ExecutionCount));
			component.OnGameplayEffectApplied(Owner, activeEffect.GameplayEffectEvaluatedData);
		}
	}

	private void RemoveStackOrUnapply(ActiveGameplayEffect? effectToRemove, bool forceUnapply)
	{
		if (effectToRemove is null)
		{
			return;
		}

		if (!forceUnapply &&
			effectToRemove.EffectData.StackingData.HasValue &&
			effectToRemove.EffectData.StackingData.Value.ExpirationPolicy ==
			StackExpirationPolicy.RemoveSingleStackAndRefreshDuration)
		{
			effectToRemove.RemoveStack();
			effectToRemove.RemainingDuration = effectToRemove.GameplayEffectEvaluatedData.Duration;

			if (effectToRemove.StackCount == 0)
			{
				RemoveActiveGameplayEffect(effectToRemove);
			}

			return;
		}

		effectToRemove.Unapply();
		RemoveActiveGameplayEffect(effectToRemove);
	}

	private void RemoveActiveGameplayEffect(ActiveGameplayEffect effectToRemove)
	{
		foreach (IGameplayEffectComponent component in effectToRemove.EffectData.GameplayEffectComponents)
		{
			component.OnActiveGameplayEffectRemoved(
				Owner,
				new ActiveEffectEvaluatedData(
					effectToRemove.GameplayEffectEvaluatedData,
					effectToRemove.RemainingDuration,
					effectToRemove.NextPeriodicTick,
					effectToRemove.ExecutionCount));
		}

		_activeEffects.Remove(effectToRemove);
	}
}
