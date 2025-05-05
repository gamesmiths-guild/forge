// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayEffects.Components;

/// <summary>
/// Interface for implementing custom gameplay effect components. Components can be used to extend gameplay effects
/// functionality and implement custom conditions for application.
/// </summary>
public interface IGameplayEffectComponent
{
	/// <summary>
	/// A custom validation method for validating whether a gameplay effect can be applied or not.
	/// </summary>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effect">The gameplay effect instance.</param>
	/// <returns><see langword="true"/> if the gameplay effect can be applied;<see langword="false"/> otherwise.
	/// </returns>
	bool CanApplyGameplayEffect(in IForgeEntity target, in GameplayEffect effect)
	{
		return true;
	}

	/// <summary>
	/// Executes and implements extra functionality for when an <see cref="ActiveGameplayEffect"/> is added to a target.
	/// </summary>
	/// <remarks>
	/// Note that only effects with duration can be added as active gameplay effects.
	/// </remarks>
	/// <param name="target">The target receiving the active gameplay effect.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being added.</param>
	/// <returns><see langword="true"/> if the applied effect remains active; <see langword="true"/> if it has been
	/// inhibited by the component during application.</returns>
	bool OnActiveGameplayEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		return true;
	}

	/// <summary>
	/// Executes and implements extra functionality for when an <see cref="ActiveGameplayEffect"/> is unapplied from a
	/// target. It's also called when a single stack is removed. The <paramref name="activeEffectEvaluatedData"/> data
	/// contains the number of stacks just before it's removed, so it's never going to be zero.
	/// </summary>
	/// <remarks>
	/// Note that only effects with duration can be unappled.
	/// </remarks>
	/// <param name="target">The target whose the active gameplay effect is being removed.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being removed.</param>>
	/// <param name="removed">Whether the active effect was completely removed or just got a stack unapply.</param>>
	void OnActiveGameplayEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for whenever a gameplay effect changes. Be it's level, modifier
	/// values, stacks or inhibition.
	/// </summary>
	/// <param name="target">The target of the gameplay effect.</param>
	/// <param name="activeEffectEvaluatedData">>The evaluated data for the active effect being changed.</param>
	void OnActiveGameplayEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when a gameplay effect is applied to a target.
	/// </summary>
	/// <remarks>
	/// Note that a gameplay effect is considered to be applied both when it's intially added and when a new stack is
	/// successfully applied. All effects, including instant effects, are considered to be applied and will trigger this
	/// method.
	/// </remarks>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the gameplay effect being applied.</param>
	void OnGameplayEffectApplied(IForgeEntity target, in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when a gameplay effect is executed on a target.
	/// </summary>
	/// <remarks>
	/// Note that only instant and periodic effects can be executed on a target.
	/// </remarks>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the gameplay effect being applied.</param>
	void OnGameplayEffectExecuted(IForgeEntity target, in GameplayEffectEvaluatedData effectEvaluatedData)
	{
		// This method is intentionally left blank.
	}
}
