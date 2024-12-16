// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
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

	private readonly IForgeEntity _owner = owner;

	/// <summary>
	/// Applies an effect to the owner of this manager.
	/// </summary>
	/// <param name="gameplayEffect">The instance of the gameplay effect to be applied.</param>
	public void ApplyEffect(GameplayEffect gameplayEffect)
	{
		if (gameplayEffect.EffectData.DurationData.Type == DurationType.Instant)
		{
			gameplayEffect.Execute(_owner);
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
			_ = stackableEffect.AddStack(gameplayEffect);

			// TODO: Trigger events when stack is applied or denied.
			return;
		}

		ApplyNewEffect(gameplayEffect);
	}

	/// <summary>
	/// Removes an application of a <see cref="GameplayEffect"/>.
	/// </summary>
	/// <param name="gameplayEffect">The instance of the gameplay effect to be removed.</param>
	public void UnapplyEffect(GameplayEffect gameplayEffect)
	{
		ActiveGameplayEffect? effectToRemove = FilterEffectsByGameplayEffect(gameplayEffect).FirstOrDefault();

		if (effectToRemove is not null)
		{
			effectToRemove.Unapply();
			_activeEffects.Remove(effectToRemove);
		}
	}

	/// <summary>
	/// Unapply an effect based on an <see cref="GameplayEffectData"/>.
	/// </summary>
	/// <remarks>
	/// This method searches for the first instance of the given effect data it can find and removes it.
	/// </remarks>
	/// <param name="gameplayEffectData">Which gameplay effect data to look for to removal.</param>
	public void UnapplyEffectData(GameplayEffectData gameplayEffectData)
	{
		ActiveGameplayEffect? effectToRemove = FilterEffectsByData(gameplayEffectData).FirstOrDefault();

		if (effectToRemove is not null)
		{
			effectToRemove.Unapply();
			_activeEffects.Remove(effectToRemove);
		}
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
		}

		_activeEffects.RemoveAll(x => x.IsExpired);
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

	/// <summary>
	/// Checks if the effect matches the stacking policy.
	/// </summary>
	/// <param name="existingEffect">The existing active effect to check.</param>
	/// <param name="newEffect">The new gameplay effect being applied.</param>
	/// <returns><see langword="true"/> if the stacking policy matches; otherwise, <see langword="false"/>.</returns>
	private static bool MatchesStackPolicy(ActiveGameplayEffect existingEffect, GameplayEffect newEffect)
	{
		Debug.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget ||
			   existingEffect.GameplayEffectEvaluatedData.GameplayEffect.Ownership.Owner == newEffect.Ownership.Owner;
	}

	/// <summary>
	/// Checks if the effect matches the stack level policy.
	/// </summary>
	/// <param name="existingEffect">The existing active effect to check.</param>
	/// <param name="newEffect">The new gameplay effect being applied.</param>
	/// <returns><see langword="true"/> if the stack level policy matches; otherwise, <see langword="false"/>.</returns>
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

	/// <summary>
	/// Finds an existing stackable effect that matches the given effect's stacking policy.
	/// </summary>
	/// <param name="gameplayEffect">The gameplay effect to check for stackable matches.</param>
	/// <returns>The matching stackable effect, or <see langword="null"/> if none are found.</returns>
	private ActiveGameplayEffect? FindStackableEffect(GameplayEffect gameplayEffect)
	{
		return FilterEffectsByData(gameplayEffect.EffectData).FirstOrDefault(x =>
			MatchesStackPolicy(x, gameplayEffect) &&
			MatchesStackLevelPolicy(x, gameplayEffect));
	}

	/// <summary>
	/// Applies a new effect to the owner.
	/// </summary>
	/// <param name="gameplayEffect">The gameplay effect to apply.</param>
	private void ApplyNewEffect(GameplayEffect gameplayEffect)
	{
		var activeEffect = new ActiveGameplayEffect(gameplayEffect, _owner);
		_activeEffects.Add(activeEffect);
		activeEffect.Apply();
	}
}
