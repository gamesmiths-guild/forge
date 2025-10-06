// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Interface for implementing custom effect components. Components can be used to extend effects
/// functionality and implement custom conditions for application.
/// </summary>
public interface IEffectComponent
{
	/// <summary>
	/// A custom validation method for validating whether a effect can be applied or not.
	/// </summary>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effect">The effect instance.</param>
	/// <returns><see langword="true"/> if the effect can be applied;<see langword="false"/> otherwise.
	/// </returns>
	bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		return true;
	}

	/// <summary>
	/// Executes and implements extra functionality for when an <see cref="ActiveEffect"/> is added to a target.
	/// </summary>
	/// <remarks>
	/// Note that only effects with duration can be added as active effects.
	/// </remarks>
	/// <param name="target">The target receiving the active effect.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being added.</param>
	/// <returns><see langword="true"/> if the applied effect remains active; <see langword="false"/> if it has been
	/// inhibited by the component during application.</returns>
	bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		return true;
	}

	/// <summary>
	/// Executes and implements extra functionality for when an <see cref="ActiveEffect"/> is unapplied from a
	/// target. It's also called when a single stack is removed. The <paramref name="activeEffectEvaluatedData"/> data
	/// contains the number of stacks just before it's removed, so it's never going to be zero.
	/// </summary>
	/// <remarks>
	/// Note that only effects with duration can be unappled.
	/// </remarks>
	/// <param name="target">The target whose the active effect is being removed.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being removed.</param>>
	/// <param name="removed">Whether the active effect was completely removed or just got a stack unapply.</param>>
	void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for whenever a effect changes. Be it's level, modifier
	/// values, stacks or inhibition.
	/// </summary>
	/// <param name="target">The target of the effect.</param>
	/// <param name="activeEffectEvaluatedData">>The evaluated data for the active effect being changed.</param>
	void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when a effect is applied to a target.
	/// </summary>
	/// <remarks>
	/// Note that a effect is considered to be applied both when it's intially added and when a new stack is
	/// successfully applied. All effects, including instant effects, are considered to be applied and will trigger this
	/// method.
	/// </remarks>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the effect being applied.</param>
	void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when a effect is executed on a target.
	/// </summary>
	/// <remarks>
	/// Note that only instant and periodic effects can be executed on a target.
	/// </remarks>
	/// <param name="target">The target of the gampleplay effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the effect being applied.</param>
	void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		// This method is intentionally left blank.
	}
}
