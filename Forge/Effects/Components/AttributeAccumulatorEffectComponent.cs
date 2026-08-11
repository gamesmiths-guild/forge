// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component that tallies how much <b>this application</b> has moved one of the target's attributes, and
/// publishes the running total as a <see cref="SetByCallerFloat"/> magnitude on its own effect, under
/// <paramref name="magnitudeTag"/>.
/// </summary>
/// <remarks>
/// <para>
/// The total is readable from anywhere a <see cref="SetByCallerFloat"/> is: an effect applied by
/// <see cref="AdditionalEffectsEffectComponent"/> with <c>copyDataFromOriginalEffect</c> inherits it, and a Statescript
/// graph reaches it through the SetByCaller magnitude resolver. That closes "a curse heals its caster for the damage it
/// dealt", lifesteal over time, "a shield reports what it absorbed", and damage-meter UI, none of which need a
/// <see cref="Calculator.CustomExecution"/>.
/// </para>
/// <para>
/// <b>It measures the attribute, not the effect's declared modifiers</b>, which is the difference between what the
/// effect meant to do and what it managed to do. Summing
/// <see cref="EffectEvaluatedData.ModifiersEvaluatedData"/> instead is wrong twice over: that array holds the intended
/// magnitudes, so it counts damage a nearly-dead target could not absorb, and it never contains the output of a
/// <see cref="Calculator.CustomExecution"/> — which is exactly where a resistance formula usually lives, since
/// <see cref="Effect.Execute"/> keeps execution modifiers in a local list that never reaches the evaluated data.
/// </para>
/// <para>
/// The measurement is taken in <see cref="OnEffectExecuted"/>, where the modifiers have landed but
/// <c>ApplyPendingValueChanges</c> has not yet run, against a baseline kept current by an
/// <see cref="EntityAttribute.OnValueChanged"/> subscription. The subscription is what keeps the difference honest:
/// it absorbs every other source touching the attribute between executions, so each measurement only ever spans this
/// effect's own change, already clamped by the attribute's <see cref="EntityAttribute.Min"/> and
/// <see cref="EntityAttribute.Max"/>.
/// </para>
/// <para>
/// Only instant and periodic effects execute, so a duration effect that is not periodic would never tally anything;
/// <see cref="EffectData"/> asserts against it.
/// </para>
/// <para>
/// One attribute per component, deliberately. To tally two — damage through a shield as well as damage to health —
/// use two components with two tags, and read them separately or add them together with two modifiers on the effect
/// that consumes them. Merging them into one total inside the component would throw away the split, and there is no
/// single sensible reading of <see cref="AccumulationPolicy.Losses"/> across a set of attributes that moved in
/// opposite directions.
/// </para>
/// <para>
/// This component maintains per-effect-instance state (the running total and an event subscription). When used in
/// <see cref="EffectData"/>, each effect application will create its own instance via <see cref="CreateInstance"/> to
/// isolate state between different effect applications. That is what an attribute on the entity could never do: two
/// curses from two casters would share it.
/// </para>
/// </remarks>
/// <param name="attribute">The fully qualified key of the attribute to measure on the target.</param>
/// <param name="magnitudeTag">The tag the running total is published under.</param>
/// <param name="policy">Which movements to count.</param>
public class AttributeAccumulatorEffectComponent(
	StringKey attribute,
	Tag magnitudeTag,
	AccumulationPolicy policy = AccumulationPolicy.Losses) : IEffectComponent
{
	private EntityAttribute? _trackedAttribute;

	private Action<EntityAttribute, int>? _handler;

	private int _baseline;

	private bool _tracking;

	/// <summary>
	/// Gets the running total this instance has accumulated so far, the same value published under
	/// <see cref="MagnitudeTag"/>.
	/// </summary>
	/// <remarks>
	/// Reachable at runtime through
	/// <c>ActiveEffectHandle.GetComponent&lt;AttributeAccumulatorEffectComponent&gt;()</c>, for a damage meter or a
	/// debug readout that wants the number without going through a <see cref="SetByCallerFloat"/>.
	/// </remarks>
	public float Total { get; private set; }

	internal StringKey Attribute { get; } = attribute;

	internal Tag MagnitudeTag { get; } = magnitudeTag;

	internal AccumulationPolicy Policy { get; } = policy;

	/// <inheritdoc/>
	public IEffectComponent CreateInstance()
	{
		// Create a new instance for each effect application to isolate the running total and the subscription.
		return new AttributeAccumulatorEffectComponent(Attribute, MagnitudeTag, Policy);
	}

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (TryStartTracking(target, activeEffectEvaluatedData.EffectEvaluatedData.Effect))
		{
			Validation.Assert(_trackedAttribute is not null, "Tracking never starts without an attribute to track.");

			// Which change it was is irrelevant: every one of them, from this effect or from anywhere else, becomes the
			// new baseline. Stored so we can unsubscribe later.
			_handler = (changedAttribute, _) => _baseline = changedAttribute.CurrentValue;
			_trackedAttribute.OnValueChanged += _handler;
		}

		return true;
	}

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		// An instant effect never becomes active, so this is its only chance to seed the tag and take a baseline before
		// it executes. It needs no subscription: it executes once, immediately, with nothing able to interleave.
		// A duration effect has already started tracking in OnActiveEffectAdded, and this hook fires again for every
		// stack, so starting is guarded to keep a stack application from resetting the total.
		TryStartTracking(target, effectEvaluatedData.Effect);
	}

	/// <inheritdoc/>
	public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (_trackedAttribute is null)
		{
			return;
		}

		int currentValue = _trackedAttribute.CurrentValue;
		int change = currentValue - _baseline;

		_baseline = currentValue;

		int accumulated = Policy switch
		{
			AccumulationPolicy.Losses => change < 0 ? -change : 0,
			AccumulationPolicy.Gains => change > 0 ? change : 0,
			_ => change,
		};

		if (accumulated == 0)
		{
			// The published total is still current, and re-publishing it would re-evaluate any effect keyed on this tag
			// for no change.
			return;
		}

		Total += accumulated;
		effectEvaluatedData.Effect.SetSetByCallerMagnitude(MagnitudeTag, Total);
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed,
		EffectRemovalReason reason)
	{
		if (!removed || _handler is null || _trackedAttribute is null)
		{
			return;
		}

		_trackedAttribute.OnValueChanged -= _handler;
		_trackedAttribute = null;
		_handler = null;
	}

	/// <inheritdoc/>
	public void OnTargetAttributesChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		if (_trackedAttribute is not null && _handler is not null)
		{
			_trackedAttribute.OnValueChanged -= _handler;
		}

		_trackedAttribute = null;

		if (!target.Attributes.TryGetAttribute(Attribute, out EntityAttribute? trackedAttribute))
		{
			return;
		}

		// Deliberately bypasses the _tracking guard, which exists to stop a stack application from resetting the
		// total. A fresh baseline is taken because the accumulator reports change since it started watching, and it
		// was not watching while the attribute was away.
		_handler ??= (changedAttribute, _) => _baseline = changedAttribute.CurrentValue;
		_trackedAttribute = trackedAttribute;
		_baseline = trackedAttribute.CurrentValue;
		trackedAttribute.OnValueChanged += _handler;
	}

	private bool TryStartTracking(IForgeEntity target, Effect effect)
	{
		if (_tracking)
		{
			return false;
		}

		_tracking = true;

		// Seeded before anything else, and seeded even when the attribute turns out to be missing: SetByCallerFloat
		// reads the dictionary with a plain indexer, so a tag nobody ever set throws rather than resolving to zero.
		// This is what lets an effect that ends before it ever executes still pay out nothing.
		effect.SetSetByCallerMagnitude(MagnitudeTag, 0);

		if (!target.Attributes.ContainsAttribute(Attribute))
		{
			return false;
		}

		_trackedAttribute = target.Attributes[Attribute];
		_baseline = _trackedAttribute.CurrentValue;

		return true;
	}
}
