// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that raises a Forge <see cref="EventData"/> at chosen points in its effect's lifetime, on the
/// target, the source, or both.
/// </summary>
/// <remarks>
/// <para>
/// This closes the Effects → Events → Statescript loop: anything that can subscribe to an <see cref="EventManager"/>
/// can now react to an effect landing, ticking, or wearing off without polling for it. Graphs need no new surface for
/// it either, since <c>EventListenerNode</c> already exists.
/// </para>
/// <para>
/// The event is raised on the bus of each entity named by <paramref name="raiseOn"/>, but its
/// <see cref="EventData.Source"/> and <see cref="EventData.Target"/> always describe the effect itself — who caused it
/// and who received it — regardless of whose bus is carrying it. A listener therefore reads the same story from either
/// side. <see cref="EventData.Payload"/> carries the raising <see cref="Effect"/>.
/// </para>
/// <para>
/// <paramref name="magnitude"/> is a full <see cref="ModifierMagnitude"/> rather than a plain number, so the event can
/// report a value that scales with level, reads an attribute, or — with
/// <see cref="MagnitudeCalculationType.SetByCaller"/> — reads a running total published by an
/// <see cref="AttributeAccumulatorEffectComponent"/> on the same effect. Omitting it reports zero.
/// </para>
/// <para>
/// Stateless: the same instance serves every application, since nothing is remembered between raises.
/// </para>
/// </remarks>
/// <param name="eventTags">The tags carried by the raised event. Subscribers match on these, so an event with none
/// reaches nobody.</param>
/// <param name="triggers">The lifecycle points at which to raise. The values combine.</param>
/// <param name="magnitude">The magnitude reported as <see cref="EventData.EventMagnitude"/>, or
/// <see langword="null"/> to report zero.</param>
/// <param name="raiseOn">The entities whose buses receive the event. Defaults to the target alone.</param>
public class RaiseEventEffectComponent(
	TagContainer eventTags,
	EffectEventTrigger triggers = EffectEventTrigger.Applied,
	ModifierMagnitude? magnitude = null,
	EffectApplicationTarget[]? raiseOn = null) : IEffectComponent
{
	private static readonly EffectApplicationTarget[] _targetOnly = [EffectApplicationTarget.Target];

	internal TagContainer EventTags { get; } = eventTags;

	internal EffectEventTrigger Triggers { get; } = triggers;

	internal ModifierMagnitude? Magnitude { get; } = magnitude;

	internal EffectApplicationTarget[] RaiseOn { get; } = raiseOn is null || raiseOn.Length == 0
		? _targetOnly
		: raiseOn;

	// An event carrying no tags matches no subscription, so it is raised into nothing. EffectData rejects it.
	internal bool HasNoEventTags => EventTags.IsEmpty;

	// A component that never raises is always a mistake rather than a deliberate no-op. EffectData rejects it.
	internal bool HasNoTrigger => Triggers == EffectEventTrigger.None;

	// Executions only happen on instant and periodic effects. EffectData rejects the combination.
	internal bool RaisesOnExecution => Triggers.HasFlag(EffectEventTrigger.Executed);

	// Removal only happens to effects that became active, which an instant one never does. EffectData rejects it.
	internal bool RaisesOnRemoval => Triggers.HasFlag(EffectEventTrigger.Removed);

	// Only a stackable effect can lose a stack and survive it. EffectData rejects the combination.
	internal bool RaisesOnStackRemoval => Triggers.HasFlag(EffectEventTrigger.StackRemoved);

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (Triggers.HasFlag(EffectEventTrigger.Applied))
		{
			Raise(target, effectEvaluatedData);
		}
	}

	/// <inheritdoc/>
	public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (Triggers.HasFlag(EffectEventTrigger.Executed))
		{
			Raise(target, effectEvaluatedData);
		}
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		// Both halves of this hook are worth reporting, and they mean different things: one stack fewer on an effect
		// that is still there, versus the effect being gone.
		EffectEventTrigger trigger = removed ? EffectEventTrigger.Removed : EffectEventTrigger.StackRemoved;

		if (Triggers.HasFlag(trigger))
		{
			Raise(target, activeEffectEvaluatedData.EffectEvaluatedData);
		}
	}

	private void Raise(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		Effect effect = effectEvaluatedData.Effect;

		var eventData = new EventData
		{
			EventTags = EventTags,
			Source = effect.Ownership.Source,
			Target = target,
			EventMagnitude = Magnitude?.GetMagnitude(
				effect,
				target,
				effectEvaluatedData.Level,
				effectEvaluatedData) ?? 0,
			Payload = effect,
		};

		foreach (EffectApplicationTarget raiseTarget in RaiseOn)
		{
			// An entity the effect does not have — a source on an effect that has none — has no bus to raise on and is
			// skipped rather than redirected back at the target.
			raiseTarget.Resolve(target, effect.Ownership)?.Events.Raise(in eventData);
		}
	}
}
