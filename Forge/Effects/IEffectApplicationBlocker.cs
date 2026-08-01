// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// A target-side veto over effect application, registered on an <see cref="EffectsManager"/> and consulted before any
/// effect is applied to its owner.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Components.IEffectComponent.CanApplyEffect"/> only ever consults the incoming effect's own components, so
/// "nothing can land on me right now" has to be configured on every effect that might arrive. A blocker inverts that:
/// the target declares once what it refuses, and effects it has never heard of are covered.
/// </para>
/// <para>
/// <see cref="Components.ImmunityEffectComponent"/> is the effect-driven implementation, but the registry is not tied
/// to effects at all — a cutscene gate or a god mode toggle can register a blocker directly.
/// </para>
/// </remarks>
public interface IEffectApplicationBlocker
{
	/// <summary>
	/// Decides whether the given effect is allowed to be applied to the owner of the manager this blocker is registered
	/// on.
	/// </summary>
	/// <remarks>
	/// Called on every application, before the effect becomes active, so it should be cheap and free of side effects.
	/// </remarks>
	/// <param name="effect">The effect about to be applied.</param>
	/// <returns><see langword="true"/> to allow the application; <see langword="false"/> to block it.</returns>
	bool AllowEffectApplication(in Effect effect);
}
