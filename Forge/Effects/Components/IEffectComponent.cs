// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Interface for implementing custom effect components. Components can be used to extend effects
/// functionality and implement custom conditions for application.
/// </summary>
/// <remarks>
/// <para>
/// Components are stored in <see cref="EffectData"/> and should be designed as stateless definitions.
/// If a component needs to maintain per-effect-instance state (such as tracking granted abilities,
/// event subscriptions, or other runtime data), it should return a new instance from
/// <see cref="CreateInstance"/> that implements the stateful behavior.
/// </para>
/// <para>
/// Stateless components can use the default <see cref="CreateInstance"/> implementation which returns
/// <c>this</c>, allowing the same component instance to be shared across all effect applications.
/// </para>
/// </remarks>
public interface IEffectComponent
{
	/// <summary>
	/// Gets which entity's attributes this component watches.
	/// </summary>
	/// <remarks>
	/// Effects register as dependents of every entity whose attributes they read live, so that a change to that
	/// entity's attribute sets reaches them. Capture definitions declare their own source, but a component that
	/// subscribes to attributes of its own accord cannot be discovered that way — naming the entity here is how it
	/// says so. The default, <see cref="AttributeCaptureSource.Target"/>, needs no registration: the target's own
	/// manager already holds the effect and rebuilds it directly.
	/// </remarks>
	AttributeCaptureSource WatchedAttributeSource => AttributeCaptureSource.Target;

	/// <summary>
	/// Creates an instance of this component for a specific effect application.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Override this method to return a new instance when your component needs to maintain
	/// per-effect-instance state. The returned instance will be used for all lifecycle callbacks
	/// for that specific effect application.
	/// </para>
	/// <para>
	/// Stateless components can use the default implementation which returns <c>this</c>.
	/// </para>
	/// </remarks>
	/// <returns>An <see cref="IEffectComponent"/> instance to use for this effect application.</returns>
	IEffectComponent CreateInstance()
	{
		return this;
	}

	/// <summary>
	/// A custom validation method for validating whether a effect can be applied or not.
	/// </summary>
	/// <param name="target">The target of the gameplay effect.</param>
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
	/// Executes and implements extra functionality for when an <see cref="ActiveEffect"/> is added to a target after
	/// all other components have processed <see cref="OnActiveEffectAdded"/> and finished evaluating.
	/// </summary>
	/// <param name="target">The target receiving the active effect.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being added.</param>
	void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when an <see cref="ActiveEffect"/> is unapplied from a
	/// target. It's also called when a single stack is removed. The <paramref name="activeEffectEvaluatedData"/> data
	/// contains the number of stacks just before it's removed, so it's never going to be zero.
	/// </summary>
	/// <remarks>
	/// Note that only effects with duration can be unapplied.
	/// </remarks>
	/// <param name="target">The target whose active effect is being removed.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect being removed.</param>
	/// <param name="removed">Whether the active effect was completely removed or just had a stack removed.</param>
	/// <param name="reason">Whether the effect ended on its own or was taken away before expiring.</param>
	void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
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
	/// Executes and implements extra functionality for when the attribute set membership of an entity this effect
	/// reads changes, so a component watching individual attributes can rebind to the ones that entity now has.
	/// </summary>
	/// <remarks>
	/// A component that subscribed to <see cref="Attributes.EntityAttribute"/> instances must drop the ones that left
	/// — they are detached and will never raise again, and holding them keeps the whole set alive — and pick up any
	/// it had been waiting for. Components that address attributes purely by key have nothing to do here.
	/// </remarks>
	/// <param name="changedEntity">The entity whose attribute set membership changed. Not necessarily the effect's
	/// target: an effect that reads a source or owner attribute is notified when that entity changes too, so a
	/// component must check which entity it is looking at before reacting.</param>
	/// <param name="activeEffectEvaluatedData">The evaluated data for the active effect.</param>
	void OnAttributeMembershipChanged(
		IForgeEntity changedEntity,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Executes and implements extra functionality for when a effect is applied to a target.
	/// </summary>
	/// <remarks>
	/// Note that a effect is considered to be applied both when it's initially added and when a new stack is
	/// successfully applied. All effects, including instant effects, are considered to be applied and will trigger this
	/// method.
	/// </remarks>
	/// <param name="target">The target of the gameplay effect.</param>
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
	/// <param name="target">The target of the gameplay effect.</param>
	/// <param name="effectEvaluatedData">The evaluated data for the effect being applied.</param>
	void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		// This method is intentionally left blank.
	}
}
