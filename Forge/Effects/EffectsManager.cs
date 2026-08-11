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
	/// <summary>
	/// How deep an effect application may cascade into further applications before the chain is treated as a cycle.
	/// </summary>
	/// <remarks>
	/// Components that apply effects — <see cref="AdditionalEffectsEffectComponent"/> above all — make it possible for
	/// A to apply B while B applies A. The limit is high enough that no honest chain of effects reaches it and low
	/// enough to stop a cycle long before the call stack does.
	/// </remarks>
	private const int MaxApplicationDepth = 16;

	private readonly CuesManager _cuesManager = cuesManager;

	private readonly List<ActiveEffect> _activeEffects = [];

	private readonly List<IEffectApplicationBlocker> _applicationBlockers = [];

	private int _applicationDepth;

	/// <summary>
	/// Event triggered whenever an effect lands on the owner, carrying its evaluated data.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The manager-wide counterpart of <see cref="IEffectComponent.OnEffectApplied"/>, with the same meaning of
	/// "applied": every effect raises it, including instant ones that never become active, and a stackable effect
	/// raises it again on each successful application.
	/// </para>
	/// <para>
	/// For newly applied effects, this is the registration phase, so the effect's attribute changes have not
	/// necessarily landed yet: an instant effect has yet to execute, and a duration effect has yet to apply its
	/// modifiers. For stack applications, the existing active effect may already have re-evaluated and applied its
	/// modifiers by the time this event fires. Use <see cref="OnEffectExecuted"/>, <see cref="OnActiveEffectAdded"/>,
	/// <see cref="OnActiveEffectChanged"/> or <see cref="Attributes.EntityAttribute.OnValueChanged"/> when you need a
	/// settled post-change view.
	/// </para>
	/// <para>
	/// For the buff-bar lifecycle — one event per active effect appearing and disappearing — use
	/// <see cref="OnActiveEffectAdded"/> and <see cref="OnActiveEffectRemoved"/> instead.
	/// </para>
	/// </remarks>
	public event Action<EffectEvaluatedData>? OnEffectApplied;

	/// <summary>
	/// Event triggered whenever an effect executes on the owner, carrying its evaluated data.
	/// </summary>
	/// <remarks>
	/// The manager-wide counterpart of <see cref="IEffectComponent.OnEffectExecuted"/>: only instant and periodic
	/// effects execute, and a periodic effect raises it on every tick. The execution has already changed the base
	/// values by this point, so handlers read post-execution attributes.
	/// </remarks>
	public event Action<EffectEvaluatedData>? OnEffectExecuted;

	/// <summary>
	/// Event triggered when an effect becomes active on the owner, carrying its handle.
	/// </summary>
	/// <remarks>
	/// Raised once per <see cref="ActiveEffect"/>, after every component has finished processing the application, so
	/// the handle already reports its settled stack count, level and inhibition state. Instant effects never become
	/// active and never raise it; a new stack on an already-active effect raises
	/// <see cref="OnActiveEffectChanged"/> rather than this.
	/// </remarks>
	public event Action<ActiveEffectHandle>? OnActiveEffectAdded;

	/// <summary>
	/// Event triggered when an active effect on the owner changes, carrying its handle.
	/// </summary>
	/// <remarks>
	/// The manager-wide counterpart of <see cref="IEffectComponent.OnActiveEffectChanged"/>: stack count, level,
	/// re-evaluated magnitudes and inhibition all report through it. An application that changes nothing — a stack
	/// arriving at an effect already at its limit under
	/// <see cref="StackOverflowPolicy.DenyApplication"/> — does not raise it.
	/// </remarks>
	public event Action<ActiveEffectHandle>? OnActiveEffectChanged;

	/// <summary>
	/// Event triggered when an active effect is removed from the owner, carrying its handle and why it ended.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Raised once per <see cref="ActiveEffect"/>, after every component has processed the removal but before the
	/// handle is invalidated, so the handle can still be read inside the handler and reports
	/// <see cref="ActiveEffectHandle.IsValid"/> as <see langword="false"/> afterwards. Losing a single stack of an
	/// effect that survives raises <see cref="OnActiveEffectChanged"/> instead.
	/// </para>
	/// <para>
	/// The owner's attributes still carry the effect's modifiers at this point; they are released immediately after,
	/// and <see cref="Attributes.EntityAttribute.OnValueChanged"/> is the seam for observing that.
	/// </para>
	/// </remarks>
	public event Action<ActiveEffectHandle, EffectRemovalReason>? OnActiveEffectRemoved;

	/// <summary>
	/// Event triggered when a registered <see cref="IEffectApplicationBlocker"/> denies an effect application. Carries
	/// the effect that was blocked and the blocker that denied it.
	/// </summary>
	/// <remarks>
	/// Effects denied by their own <see cref="IEffectComponent.CanApplyEffect"/> never reach the blockers and do not
	/// raise this event.
	/// </remarks>
	public event Action<Effect, IEffectApplicationBlocker>? OnEffectApplicationBlocked;

	/// <summary>
	/// Event triggered when an effect reaches an active effect it would stack onto and its <see cref="StackingData"/>
	/// refuses the application. Carries the effect that was refused and the handle of the active effect that refused
	/// it.
	/// </summary>
	/// <remarks>
	/// The stacking counterpart of <see cref="OnEffectApplicationBlocked"/>, covering every stacking-driven denial:
	/// <see cref="StackOverflowPolicy.DenyApplication"/> at the stack limit,
	/// <see cref="StackingData.LevelDenialPolicy"/>, and <see cref="StackOwnerDenialPolicy.DenyIfDifferent"/>. These
	/// applications are otherwise invisible, since <see cref="ApplyEffect(Effect)"/> still returns the handle of the
	/// effect already in place.
	/// </remarks>
	public event Action<Effect, ActiveEffectHandle>? OnEffectStackDenied;

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
		return ApplyEffectInternal(effect, applicationContext: null);
	}

	/// <summary>
	/// Applies an effect to the owner of this manager with custom context data.
	/// </summary>
	/// <typeparam name="TData">The type of custom data to pass through the effect pipeline.</typeparam>
	/// <param name="effect">The instance of the effect to be applied.</param>
	/// <param name="contextData">Custom data to pass through the effect pipeline to CustomCalculators and
	/// CustomExecutions.</param>
	/// <returns>A handle to the applied effect if it was successfully applied as an <see cref="ActiveEffect"/>.
	/// </returns>
	/// <remarks>
	/// The context data can be accessed in <see cref="Calculator.CustomExecution"/> or
	/// <see cref="Calculator.CustomModifierMagnitudeCalculator"/> via
	/// <see cref="EffectEvaluatedData.TryGetContextData{TData}(out TData)"/>.
	/// </remarks>
	public ActiveEffectHandle? ApplyEffect<TData>(Effect effect, TData contextData)
	{
		return ApplyEffectInternal(effect, new EffectApplicationContext<TData>(contextData));
	}

	/// <summary>
	/// Removes an application of an <see cref="ActiveEffect"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <param name="activeEffect">The instance of the active effect to be removed.</param>
	/// <param name="forceRemoval">Forces removal even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void RemoveEffect(ActiveEffectHandle activeEffect, bool forceRemoval = false)
	{
		RemoveStackOrUnapply(activeEffect.ActiveEffect, forceRemoval);
	}

	/// <summary>
	/// Removes a set number of stacks from an <see cref="ActiveEffect"/>, removing it outright once nothing is left.
	/// </summary>
	/// <remarks>
	/// The handle counterpart of
	/// <see cref="RemoveEffects(EffectQuery, int, IReadOnlyCollection{ActiveEffectHandle})"/>: partial removal is
	/// explicit rather than driven by <see cref="StackExpirationPolicy"/>, and it never refreshes the remaining
	/// duration of what survives.
	/// </remarks>
	/// <param name="activeEffect">The instance of the active effect to take stacks from.</param>
	/// <param name="stacksToRemove">How many stacks to remove. Any negative value removes the effect entirely,
	/// regardless of its stack count.</param>
	public void RemoveEffect(ActiveEffectHandle activeEffect, int stacksToRemove)
	{
		if (activeEffect.ActiveEffect is null || stacksToRemove == 0)
		{
			return;
		}

		RemoveStacks(activeEffect.ActiveEffect, stacksToRemove);
	}

	/// <summary>
	/// Removes an application of a <see cref="Effect"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <param name="effect">The instance of the effect to be removed.</param>
	/// <param name="forceRemoval">Forces removal even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void RemoveEffect(Effect effect, bool forceRemoval = false)
	{
		RemoveStackOrUnapply(FilterEffectsByEffect(effect).FirstOrDefault(), forceRemoval);
	}

	/// <summary>
	/// Removes an effect based on an <see cref="EffectData"/> or an stack if it's a stackable effect with
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.
	/// </summary>
	/// <remarks>
	/// This method searches for the first instance of the given effect data it can find and removes it.
	/// </remarks>
	/// <param name="effectData">Which effect data to look for to removal.</param>
	/// /// <param name="forceRemoval">Forces removal even if <see cref="StackExpirationPolicy"/> is set to
	/// <see cref="StackExpirationPolicy.RemoveSingleStackAndRefreshDuration"/>.</param>
	public void RemoveEffectData(EffectData effectData, bool forceRemoval = false)
	{
		RemoveStackOrUnapply(FilterEffectsByData(effectData).FirstOrDefault(), forceRemoval);
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
		ActiveEffect[] effectsToUpdate = [.. _activeEffects];
		foreach (ActiveEffect effect in effectsToUpdate)
		{
			effect.Update(deltaTime);
		}

		foreach (ActiveEffect expiredEffect in effectsToUpdate.Where(x => x.IsExpired).ToArray())
		{
			RemoveActiveEffect(expiredEffect, EffectRemovalReason.Expired);
		}
	}

	/// <summary>
	/// Queries and gets the stack data for the active applications of a given <see cref="EffectData"/>.
	/// </summary>
	/// <param name="effectData">Which effect to query for.</param>
	/// <returns>An enumerable of <see cref="EffectStackInstanceData"/> grouped by their stack configuration.</returns>
	public IEnumerable<EffectStackInstanceData> GetEffectStackData(EffectData effectData)
	{
		IEnumerable<ActiveEffect> filteredEffects = FilterEffectsByData(effectData);
		return ConvertToStackInstanceData(filteredEffects);
	}

	/// <summary>
	/// Queries and gets the stack data for the active effects matching the given <see cref="EffectQuery"/>.
	/// </summary>
	/// <param name="query">The query the active effects must match. An empty query matches every active effect.
	/// </param>
	/// <returns>An enumerable of <see cref="EffectStackInstanceData"/> grouped by their stack configuration.</returns>
	public IEnumerable<EffectStackInstanceData> GetEffectStackData(EffectQuery query)
	{
		return ConvertToStackInstanceData(FilterEffectsByQuery(query));
	}

	/// <summary>
	/// Queries and gets the handles for all effects currently active on the owner.
	/// </summary>
	/// <returns>The handles for all currently active effects.</returns>
	public IEnumerable<ActiveEffectHandle> GetActiveEffects()
	{
		return _activeEffects.Select(x => x.Handle);
	}

	/// <summary>
	/// Queries and gets the handles for all active effects matching the given <see cref="EffectQuery"/>.
	/// </summary>
	/// <remarks>
	/// To select the applications of one specific effect, set <see cref="EffectQuery.EffectDefinition"/>.
	/// </remarks>
	/// <param name="query">The query the active effects must match. An empty query matches every active effect.
	/// </param>
	/// <returns>The handles for the currently active effects matching the given query.</returns>
	public IEnumerable<ActiveEffectHandle> GetActiveEffects(EffectQuery query)
	{
		return FilterEffectsByQuery(query).Select(x => x.Handle);
	}

	/// <summary>
	/// Checks whether any effect matching the given <see cref="EffectQuery"/> is currently active on the owner.
	/// </summary>
	/// <remarks>
	/// Cheaper than <see cref="GetActiveEffects(EffectQuery)"/> when only the answer matters, since it stops at the
	/// first match.
	/// </remarks>
	/// <param name="query">The query the active effects must match. An empty query matches every active effect.
	/// </param>
	/// <returns><see langword="true"/> if at least one active effect matches; <see langword="false"/> otherwise.
	/// </returns>
	public bool HasAnyActiveEffect(EffectQuery query)
	{
		return _activeEffects.Exists(x => query.Matches(x.Handle));
	}

	/// <summary>
	/// Removes every active effect matching the given <see cref="EffectQuery"/>.
	/// </summary>
	/// <remarks>
	/// Unlike <see cref="RemoveEffect(ActiveEffectHandle, bool)"/>, partial removal here is explicit rather than driven
	/// by <see cref="StackExpirationPolicy"/>, and it never refreshes the remaining duration.
	/// </remarks>
	/// <param name="query">The query the active effects must match. An empty query matches every active effect.
	/// </param>
	/// <param name="stacksToRemove">How many stacks to remove from each matching effect. Any negative value removes
	/// the effects entirely, regardless of their stack count.</param>
	/// <param name="ignoredHandles">Handles that are never removed, even when they match the query.</param>
	/// <returns>The number of active effects that matched the query.</returns>
	public int RemoveEffects(
		EffectQuery query,
		int stacksToRemove = -1,
		IReadOnlyCollection<ActiveEffectHandle>? ignoredHandles = null)
	{
		if (stacksToRemove == 0)
		{
			return 0;
		}

		// Snapshotting is required: removal callbacks can apply or remove further effects.
		ActiveEffect[] matchingEffects =
			[.. _activeEffects.Where(x => query.Matches(x.Handle, ignoredHandles))];

		foreach (ActiveEffect matchingEffect in matchingEffects)
		{
			RemoveStacks(matchingEffect, stacksToRemove);
		}

		return matchingEffects.Length;
	}

	/// <summary>
	/// Registers a blocker that gets to veto every effect applied to the owner from now on.
	/// </summary>
	/// <remarks>
	/// Registering the same blocker twice has no effect, so a blocker is always removed by a single
	/// <see cref="UnregisterApplicationBlocker(IEffectApplicationBlocker)"/> call.
	/// </remarks>
	/// <param name="blocker">The blocker to be consulted before each application.</param>
	public void RegisterApplicationBlocker(IEffectApplicationBlocker blocker)
	{
		if (_applicationBlockers.Contains(blocker))
		{
			return;
		}

		_applicationBlockers.Add(blocker);
	}

	/// <summary>
	/// Unregisters a previously registered blocker, which stops vetoing applications immediately.
	/// </summary>
	/// <param name="blocker">The blocker to stop consulting.</param>
	public void UnregisterApplicationBlocker(IEffectApplicationBlocker blocker)
	{
		_applicationBlockers.Remove(blocker);
	}

	internal void OnEffectExecuted_InternalCall(
		EffectEvaluatedData executedEffectEvaluatedData,
		IEffectComponent[]? componentInstances)
	{
		foreach (IEffectComponent component in componentInstances
			?? executedEffectEvaluatedData.Effect.EffectData.EffectComponents)
		{
			component.OnEffectExecuted(Owner, in executedEffectEvaluatedData);
		}

		OnEffectExecuted?.Invoke(executedEffectEvaluatedData);

		_cuesManager.ExecuteCues(in executedEffectEvaluatedData);
	}

	internal void OnActiveEffectUnapplied_InternalCall(ActiveEffect removedEffect, EffectRemovalReason reason)
	{
		foreach (IEffectComponent component in removedEffect.ComponentInstances)
		{
			component.OnActiveEffectUnapplied(
				Owner,
				new ActiveEffectEvaluatedData(
					removedEffect.Handle,
					removedEffect.EffectEvaluatedData,
					removedEffect.RemainingDuration,
					removedEffect.NextPeriodicTick,
					removedEffect.ExecutionCount),
				false,
				reason);
		}
	}

	internal void OnActiveEffectChanged_InternalCall(ActiveEffect removedEffect)
	{
		foreach (IEffectComponent component in removedEffect.ComponentInstances)
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

		OnActiveEffectChanged?.Invoke(removedEffect.Handle);
	}

	internal void TriggerCuesUpdate_InternalCall(in EffectEvaluatedData effectEvaluatedData)
	{
		_cuesManager.UpdateCues(in effectEvaluatedData);
	}

	internal void RemoveActiveEffect_InternalCall(ActiveEffect effect)
	{
		RemoveActiveEffect(effect, EffectRemovalReason.Expired);
	}

	internal ActiveEffect? FindActiveEffectByData(EffectData effectData)
	{
		return FilterEffectsByData(effectData).FirstOrDefault();
	}

	/// <summary>
	/// Unwinds every active effect's modifiers, runs a change to the entity's attribute set membership, then
	/// re-evaluates and re-applies them. The unwind has to precede the detach, or a departing attribute keeps this
	/// entity's modifiers baked into its channels and overrides and comes back dirty if the set is re-added. Every
	/// effect participates, not just those whose modifiers name a departing attribute, because an effect can depend on
	/// one indirectly through an attribute-based magnitude, a custom calculator or its own duration.
	/// </summary>
	/// <param name="applyChange">The action that applies the attribute set membership change.</param>
	internal void RebuildAroundAttributeChange(Action applyChange)
	{
		ActiveEffect[] activeEffects = [.. _activeEffects];

		foreach (ActiveEffect activeEffect in activeEffects)
		{
			activeEffect.Unapply(reApplication: true);
		}

		applyChange();

		foreach (ActiveEffect activeEffect in activeEffects)
		{
			activeEffect.RebuildAfterAttributeChange();
		}

		// Only now, so an attribute the entity keeps sees the net change rather than the unwind and the re-apply as
		// two separate ones. Attributes that are leaving were flushed by the change itself, while they still could be.
		Owner.Attributes.ApplyPendingValueChanges();

		// Second pass, so a component that reacts by inhibiting or removing sees every effect already settled against
		// the new membership rather than a half-rebuilt entity.
		foreach (ActiveEffect activeEffect in activeEffects)
		{
			// An earlier component may have removed this one.
			if (!_activeEffects.Contains(activeEffect))
			{
				continue;
			}

			foreach (IEffectComponent component in activeEffect.ComponentInstances)
			{
				component.OnTargetAttributesChanged(
					Owner,
					new ActiveEffectEvaluatedData(
						activeEffect.Handle,
						activeEffect.EffectEvaluatedData,
						activeEffect.RemainingDuration,
						activeEffect.NextPeriodicTick,
						activeEffect.ExecutionCount));
			}
		}
	}

	internal ActiveEffectHandle? ApplyEffectInternal(Effect effect, EffectApplicationContext? applicationContext)
	{
		return ApplyEffectInternal(effect, applicationContext, out _);
	}

	/// <summary>
	/// Applies an effect and reports whether it actually landed.
	/// </summary>
	/// <remarks>
	/// A returned handle cannot answer this on its own: an instant effect has no <see cref="ActiveEffect"/> to hand
	/// back, so it returns <see langword="null"/> whether it executed or was turned away by a component's
	/// <see cref="IEffectComponent.CanApplyEffect"/> or by a registered <see cref="IEffectApplicationBlocker"/>.
	/// Callers that must know — an ability committing its cost, say — need this instead of re-running those checks up
	/// front, which would fire the blocked notification twice and burn through a single-use immunity.
	/// </remarks>
	/// <param name="effect">The instance of the effect to be applied.</param>
	/// <param name="activeEffectHandle">A handle to the applied effect when it became an <see cref="ActiveEffect"/>.
	/// </param>
	/// <returns><see langword="true"/> when the effect was applied.</returns>
	internal bool TryApplyEffect(Effect effect, out ActiveEffectHandle? activeEffectHandle)
	{
		activeEffectHandle = ApplyEffectInternal(effect, applicationContext: null, out bool applied);
		return applied;
	}

	private static bool MatchesStackPolicy(ActiveEffect existingEffect, Effect newEffect)
	{
		Validation.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget
			|| existingEffect.EffectEvaluatedData.Effect.Ownership.Owner == newEffect.Ownership.Owner;
	}

	private static bool MatchesStackLevelPolicy(ActiveEffect existingEffect, Effect newEffect)
	{
		Validation.Assert(
			newEffect.EffectData.StackingData.HasValue,
			"StackingData should always be valid at this point.");

		return newEffect.EffectData.StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels
			|| existingEffect.EffectEvaluatedData.Effect.Level == newEffect.Level;
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

	private ActiveEffectHandle? ApplyEffectInternal(
		Effect effect,
		EffectApplicationContext? applicationContext,
		out bool applied)
	{
		// Applications can cascade: a component reacting to one effect landing can apply another, which can apply
		// another. The chain is cut here rather than left to overflow the stack, so a build with validation disabled
		// drops the offending application instead of taking the process down with it.
		if (_applicationDepth >= MaxApplicationDepth)
		{
			Validation.Fail(
				$"Effect application exceeded {MaxApplicationDepth} levels of nesting while applying " +
				$"'{effect.EffectData.Name}', which means two or more effects are applying each other in a cycle. " +
				"Break the cycle, usually by gating one of the applications on a tag the other grants.");

			applied = false;
			return null;
		}

		_applicationDepth++;

		try
		{
			return ApplyEffectUnguarded(effect, applicationContext, out applied);
		}
		finally
		{
			_applicationDepth--;
		}
	}

	private ActiveEffectHandle? ApplyEffectUnguarded(
		Effect effect,
		EffectApplicationContext? applicationContext,
		out bool applied)
	{
		applied = false;

		if (!effect.CanApply(Owner))
		{
			return null;
		}

		if (IsApplicationBlocked(effect))
		{
			return null;
		}

		if (effect.EffectData.DurationData.DurationType == DurationType.Instant)
		{
			var evaluatedData = new EffectEvaluatedData(effect, Owner, applicationContext: applicationContext);

			// Create component instances for instant effects to ensure stateful components
			IEffectComponent[] definitions = effect.EffectData.EffectComponents;
			var componentInstances = new IEffectComponent[definitions.Length];
			for (int i = 0; i < definitions.Length; i++)
			{
				componentInstances[i] = definitions[i].CreateInstance();
			}

			foreach (IEffectComponent component in componentInstances)
			{
				component.OnEffectApplied(Owner, in evaluatedData);
			}

			OnEffectApplied?.Invoke(evaluatedData);

			Effect.Execute(in evaluatedData, componentInstances);
			applied = true;
			return null;
		}

		if (!effect.EffectData.StackingData.HasValue)
		{
			applied = true;
			return ApplyNewEffect(effect, applicationContext).Handle;
		}

		ActiveEffect? stackableEffect = FindStackableEffect(effect);

		if (stackableEffect is not null)
		{
			bool successfulApplication = stackableEffect.AddStack(effect);

			if (successfulApplication)
			{
				foreach (IEffectComponent component in stackableEffect.ComponentInstances)
				{
					component.OnEffectApplied(Owner, stackableEffect.EffectEvaluatedData);
				}

				OnEffectApplied?.Invoke(stackableEffect.EffectEvaluatedData);
			}
			else
			{
				OnEffectStackDenied?.Invoke(effect, stackableEffect.Handle);
			}

			// A denied stack hands back the existing handle but changed nothing, so it is not an application.
			applied = successfulApplication;
			return stackableEffect.Handle;
		}

		applied = true;
		return ApplyNewEffect(effect, applicationContext).Handle;
	}

	private bool IsApplicationBlocked(Effect effect)
	{
		// Indexed rather than foreach so that a blocker unregistering itself while being consulted — an immunity effect
		// that removes itself on its first block, say — doesn't invalidate the iteration. Costs nothing when the list
		// is empty, which is the common case.
		int i = 0;

		while (i < _applicationBlockers.Count)
		{
			IEffectApplicationBlocker blocker = _applicationBlockers[i];

			if (!blocker.AllowEffectApplication(in effect))
			{
				OnEffectApplicationBlocked?.Invoke(effect, blocker);
				return true;
			}

			// Advance only when the list didn't shift underneath: a blocker that unregistered itself, or an earlier
			// one, during the call moved its successor into this index, and skipping it would let through an effect
			// that successor would have vetoed.
			if (i < _applicationBlockers.Count && ReferenceEquals(_applicationBlockers[i], blocker))
			{
				i++;
			}
		}

		return false;
	}

	private IEnumerable<ActiveEffect> FilterEffectsByData(EffectData effectData)
	{
		return _activeEffects.Where(x => x.EffectEvaluatedData.Effect.EffectData == effectData);
	}

	private IEnumerable<ActiveEffect> FilterEffectsByEffect(Effect effect)
	{
		return _activeEffects.Where(x => x.EffectEvaluatedData.Effect == effect);
	}

	private IEnumerable<ActiveEffect> FilterEffectsByQuery(EffectQuery query)
	{
		return _activeEffects.Where(x => query.Matches(x.Handle));
	}

	private ActiveEffect? FindStackableEffect(Effect effect)
	{
		return FilterEffectsByData(effect.EffectData).FirstOrDefault(x =>
			MatchesStackPolicy(x, effect) &&
			MatchesStackLevelPolicy(x, effect));
	}

	private ActiveEffect ApplyNewEffect(Effect effect, EffectApplicationContext? applicationContext)
	{
		var activeEffect = new ActiveEffect(effect, Owner, applicationContext);
		_activeEffects.Add(activeEffect);

		bool remainActive = true;

		foreach (IEffectComponent component in activeEffect.ComponentInstances)
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

		OnEffectApplied?.Invoke(activeEffect.EffectEvaluatedData);

		EffectEvaluatedData effectEvaluatedData = activeEffect.EffectEvaluatedData;

		bool triggerApplyCuesEarly = effect.EffectData.PeriodicData.HasValue
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

		foreach (IEffectComponent component in activeEffect.ComponentInstances)
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

		// A component reacting to the application can take the effect straight back off — an effect it applies from
		// OnActiveEffectAdded removing this one. Announcing an addition that already ended would leave every listener
		// holding an entry nothing ever removes, so the already-raised removal is left as the last word.
		if (activeEffect.Handle.IsValid)
		{
			OnActiveEffectAdded?.Invoke(activeEffect.Handle);
		}

		return activeEffect;
	}

	private void RemoveStackOrUnapply(ActiveEffect? effectToRemove, bool forceRemoval)
	{
		if (effectToRemove is null)
		{
			return;
		}

		if (!forceRemoval
			&& effectToRemove.EffectData.StackingData.HasValue
			&& effectToRemove.EffectData.StackingData.Value.ExpirationPolicy
			== StackExpirationPolicy.RemoveSingleStackAndRefreshDuration)
		{
			effectToRemove.RemoveStack(EffectRemovalReason.Removed);
			effectToRemove.RemainingDuration = effectToRemove.EffectEvaluatedData.Duration;

			if (effectToRemove.StackCount == 0)
			{
				RemoveActiveEffect(effectToRemove, EffectRemovalReason.Removed);
			}

			return;
		}

		effectToRemove.Unapply();

		// This method is only ever reached through the public removal API, so the effect never got the chance to expire
		// on its own, regardless of its duration type.
		RemoveActiveEffect(effectToRemove, EffectRemovalReason.Removed);
	}

	private void RemoveStacks(ActiveEffect effectToRemove, int stacksToRemove)
	{
		if (stacksToRemove < 0)
		{
			effectToRemove.Unapply();
			RemoveActiveEffect(effectToRemove, EffectRemovalReason.Removed);
			return;
		}

		for (int i = 0; i < stacksToRemove && effectToRemove.StackCount > 0; i++)
		{
			effectToRemove.RemoveStack(EffectRemovalReason.Removed);

			if (effectToRemove.StackCount == 0)
			{
				RemoveActiveEffect(effectToRemove, EffectRemovalReason.Removed);
				return;
			}
		}
	}

	private void RemoveActiveEffect(ActiveEffect effectToRemove, EffectRemovalReason reason)
	{
		if (!_activeEffects.Contains(effectToRemove))
		{
			return;
		}

		_activeEffects.Remove(effectToRemove);

		EffectEvaluatedData effectEvaluatedData = effectToRemove.EffectEvaluatedData;

		foreach (IEffectComponent component in effectToRemove.ComponentInstances)
		{
			component.OnActiveEffectUnapplied(
				Owner,
				new ActiveEffectEvaluatedData(
					effectToRemove.Handle,
					effectEvaluatedData,
					effectToRemove.RemainingDuration,
					effectToRemove.NextPeriodicTick,
					effectToRemove.ExecutionCount),
				true,
				reason);
		}

		// Raised before the handle is freed so that handlers can still read what ended, and not before the component
		// callbacks so that the effect's own components — the ones that can still change the outcome — go first.
		OnActiveEffectRemoved?.Invoke(effectToRemove.Handle, reason);

		effectToRemove.Handle.Free();

		effectToRemove.EffectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();

		_cuesManager.RemoveCues(in effectEvaluatedData, reason == EffectRemovalReason.Removed);
	}
}
