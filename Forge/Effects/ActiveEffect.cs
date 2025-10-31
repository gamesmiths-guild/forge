// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// Represents an active effect that is currently affecting an entity.
/// </summary>
internal sealed class ActiveEffect
{
	private const double Epsilon = 0.00001;

	private readonly HashSet<Tag> _nonSnapshotSetByCallerTags;

	private double _internalTime;

	internal ActiveEffectHandle Handle { get; }

	internal EffectEvaluatedData EffectEvaluatedData { get; }

	internal bool IsInhibited { get; private set; }

	internal double RemainingDuration { get; set; }

	internal double NextPeriodicTick { get; private set; }

	internal int ExecutionCount { get; private set; }

	internal int StackCount { get; private set; }

	internal bool IsExpired => EffectData.DurationData.DurationType ==
		DurationType.HasDuration &&
		RemainingDuration <= 0;

	internal EffectData EffectData => EffectEvaluatedData.Effect.EffectData;

	internal Effect Effect => EffectEvaluatedData.Effect;

	internal ActiveEffect(Effect effect, IForgeEntity target)
	{
		Handle = new ActiveEffectHandle(this);

		if (effect.EffectData.StackingData.HasValue)
		{
			StackCount = effect.EffectData.StackingData.Value.InitialStack.GetValue(effect.Level);
		}
		else
		{
			StackCount = 1;
		}

		EffectEvaluatedData = new EffectEvaluatedData(effect, target, StackCount);

		_nonSnapshotSetByCallerTags = [];

		ModifierMagnitude? durationMagnitude = effect.EffectData.DurationData.DurationMagnitude;
		if (durationMagnitude.HasValue
			&& durationMagnitude.Value.MagnitudeCalculationType == MagnitudeCalculationType.SetByCaller
			&& !durationMagnitude.Value.SetByCallerFloat!.Value.Snapshot)
		{
			_nonSnapshotSetByCallerTags.Add(
				effect.EffectData.DurationData.DurationMagnitude!.Value.SetByCallerFloat!.Value.Tag);
		}

		for (var i = 0; i < EffectEvaluatedData.Effect.EffectData.Modifiers.Length; i++)
		{
			SetByCallerFloat? setByCallerFloat =
				EffectEvaluatedData.Effect.EffectData.Modifiers[i].Magnitude.SetByCallerFloat;
			if (setByCallerFloat.HasValue && !setByCallerFloat.Value.Snapshot)
			{
				_nonSnapshotSetByCallerTags.Add(setByCallerFloat.Value.Tag);
			}
		}
	}

	internal void Apply(bool reApplication = false, bool inhibited = false)
	{
		if (!reApplication)
		{
			ExecutionCount = 0;
			_internalTime = 0;
			IsInhibited = inhibited;
			RemainingDuration = EffectEvaluatedData.Duration;

			if (!EffectData.SnapshotLevel)
			{
				Effect.OnLevelChanged += Effect_OnLevelChanged;
			}

			foreach (EntityAttribute attribute in EffectEvaluatedData.AttributesToCapture)
			{
				attribute.OnValueChanged += Attribute_OnValueChanged;
			}

			Effect.OnSetByCallerFloatChanged += Effect_OnSetByCallerFloatChanged;
		}

		if (EffectData.PeriodicData.HasValue)
		{
			if (EffectData.PeriodicData.Value.ExecuteOnApplication &&
				!reApplication && !IsInhibited)
			{
				Execute();
			}

			if (!reApplication)
			{
				NextPeriodicTick = EffectEvaluatedData.Period;
			}
		}
		else if (!IsInhibited)
		{
			ApplyModifiers();
		}
	}

	internal void Unapply(bool reApplication = false)
	{
		if (!EffectData.PeriodicData.HasValue && !IsInhibited)
		{
			ApplyModifiers(true);
		}

		if (!reApplication)
		{
			StackCount = 0;

			foreach (EntityAttribute attribute in EffectEvaluatedData.AttributesToCapture)
			{
				attribute.OnValueChanged -= Attribute_OnValueChanged;
			}

			if (!EffectData.SnapshotLevel)
			{
				Effect.OnLevelChanged -= Effect_OnLevelChanged;
			}

			Effect.OnSetByCallerFloatChanged -= Effect_OnSetByCallerFloatChanged;
		}
	}

	internal bool AddStack(Effect effect, int stacks = 1)
	{
		Validation.Assert(
			EffectData.StackingData.HasValue,
			"StackingData should never be null at this point.");

		Validation.Assert(
			stacks > 0,
			"Number of stacks should be higher than 1.");

		var hasChanges = false;
		var resetStacks = false;

		StackingData stackingData = EffectData.StackingData.Value;
		var evaluatedLevel = EffectEvaluatedData.Level;

		// We have to evaluate level before checking the stack count since the level could change.
		if (stackingData.LevelDenialPolicy.HasValue)
		{
			Validation.Assert(
				stackingData.LevelOverridePolicy.HasValue,
				"LevelOverridePolicy should never be null at this point.");

			// Determine the relationship.
			LevelComparison relation;
			if (effect.Level > evaluatedLevel)
			{
				relation = LevelComparison.Higher;
			}
			else if (effect.Level < evaluatedLevel)
			{
				relation = LevelComparison.Lower;
			}
			else
			{
				relation = LevelComparison.Equal;
			}

			// Check if the relevant flag is set in the denial policy.
			if ((stackingData.LevelDenialPolicy.Value & relation) != 0)
			{
				return false;
			}

			if ((stackingData.LevelOverridePolicy.Value & relation) != 0)
			{
				Validation.Assert(
					stackingData.LevelOverrideStackCountPolicy.HasValue,
					"LevelOverrideStackCountPolicy should never be null at this point.");

				evaluatedLevel = effect.Level;
				hasChanges = true;

				resetStacks = stackingData.LevelOverrideStackCountPolicy.Value ==
					StackLevelOverrideStackCountPolicy.ResetStacks;
			}
		}

		var stackLimit = stackingData.StackLimit.GetValue(evaluatedLevel);

		if (StackCount == stackLimit &&
			stackingData.OverflowPolicy == StackOverflowPolicy.DenyApplication)
		{
			return false;
		}

		Effect evaluatedEffect = Effect;

		if (stackingData.OwnerDenialPolicy.HasValue)
		{
			if (stackingData.OwnerDenialPolicy.Value == StackOwnerDenialPolicy.DenyIfDifferent &&
				Effect.Ownership.Owner != effect.Ownership.Owner)
			{
				return false;
			}

			if (stackingData.OwnerOverridePolicy == StackOwnerOverridePolicy.Override &&
				Effect.Ownership.Owner != effect.Ownership.Owner)
			{
				evaluatedEffect = effect;
				hasChanges = true;

				Validation.Assert(
					stackingData.OwnerOverrideStackCountPolicy.HasValue,
					"OwnerOverrideStackCountPolicy should never be null at this point.");

				switch (stackingData.OwnerOverrideStackCountPolicy.Value)
				{
					case StackOwnerOverrideStackCountPolicy.ResetStacks:
						resetStacks = true;
						break;

					case StackOwnerOverrideStackCountPolicy.IncreaseStacks:
						break;
				}
			}
		}

		// It can be a successful application and still not increase stack count.
		// In some cases we can even skip re-application.
		if (resetStacks)
		{
			var initialStack = stackingData.InitialStack.GetValue(evaluatedLevel);

			if (StackCount != initialStack)
			{
				StackCount = initialStack;
				hasChanges = true;
			}
		}
		else if (StackCount < stackLimit)
		{
			StackCount = Math.Min(StackCount + stacks, stackLimit);
			hasChanges = true;
		}

		if (hasChanges)
		{
			ReapplyEffect(evaluatedEffect, evaluatedLevel, true);
		}
		else
		{
			EffectEvaluatedData effectEvaluatedData = EffectEvaluatedData;
			effectEvaluatedData.Target.EffectsManager.TriggerCuesUpdate_InternalCall(in effectEvaluatedData);
		}

		if (stackingData.ApplicationRefreshPolicy == StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication)
		{
			RemainingDuration = EffectEvaluatedData.Duration;
		}

		if (stackingData.ApplicationResetPeriodPolicy == StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication)
		{
			_internalTime = 0;
			NextPeriodicTick = EffectEvaluatedData.Period;
		}

		if (stackingData.ExecuteOnSuccessfulApplication == true && !IsInhibited)
		{
			Execute();
		}

		return true;
	}

	internal void RemoveStack()
	{
		var removed = StackCount == 1;

		if (removed)
		{
			Unapply();
			return;
		}

		EffectEvaluatedData.Target.EffectsManager.OnActiveEffectUnapplied_InternalCall(this);

		StackCount--;
		ReapplyEffect(Effect, isStackingCall: true);
	}

	internal void Update(double deltaTime)
	{
		if (EffectData.DurationData.DurationType == DurationType.HasDuration)
		{
			RemainingDuration -= deltaTime;

			if (IsExpired)
			{
				ExecutePeriodicEffects(deltaTime + RemainingDuration);

				if (EffectData.StackingData.HasValue &&
					EffectData.StackingData.Value.ExpirationPolicy ==
					StackExpirationPolicy.RemoveSingleStackAndRefreshDuration)
				{
					while (StackCount >= 1 && RemainingDuration <= Epsilon)
					{
						RemoveStack();

#pragma warning disable S2589 // Boolean expressions should not be gratuitous
						if (StackCount > 0)
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
						{
							var periodicDelta = Math.Min(-RemainingDuration, EffectEvaluatedData.Duration);
							ExecutePeriodicEffects(periodicDelta);
							RemainingDuration += EffectEvaluatedData.Duration;
						}
					}

					return;
				}

				Unapply();
			}
			else
			{
				ExecutePeriodicEffects(deltaTime);
			}
		}
		else
		{
			ExecutePeriodicEffects(deltaTime);
		}

		EffectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	internal void SetInhibit(bool value)
	{
		if (IsInhibited == value)
		{
			return;
		}

		IsInhibited = value;

		if (EffectData.PeriodicData.HasValue)
		{
			if (IsInhibited)
			{
				return;
			}

			if (EffectData.PeriodicData.Value.PeriodInhibitionRemovedPolicy
				== PeriodInhibitionRemovedPolicy.ExecuteAndResetPeriod)
			{
				Execute();
			}

			if (EffectData.PeriodicData.Value.PeriodInhibitionRemovedPolicy
				!= PeriodInhibitionRemovedPolicy.NeverReset)
			{
				NextPeriodicTick = _internalTime + EffectEvaluatedData.Period;
			}

			return;
		}

		ApplyModifiers(IsInhibited);

		EffectEvaluatedData.Target.EffectsManager.OnActiveEffectChanged_InternalCall(this);
	}

	private void ExecutePeriodicEffects(double deltaTime)
	{
		_internalTime += deltaTime;

		if (EffectData.PeriodicData.HasValue)
		{
			while (_internalTime >= NextPeriodicTick - Epsilon)
			{
				if (!IsInhibited)
				{
					Execute();
				}

				NextPeriodicTick += EffectEvaluatedData.Period;
			}
		}
	}

	private void ReapplyEffect(Effect effect, int? level = null, bool isStackingCall = false)
	{
		Unapply(true);

		EffectEvaluatedData.ReEvaluate(effect, StackCount, level);

		Apply(reApplication: true);

		EffectEvaluatedData.Target.EffectsManager.OnActiveEffectChanged_InternalCall(this);

		EffectEvaluatedData effectEvaluatedData = EffectEvaluatedData;

		if (!Effect.EffectData.SuppressStackingCues || !isStackingCall)
		{
			EffectEvaluatedData.Target.EffectsManager.TriggerCuesUpdate_InternalCall(in effectEvaluatedData);
		}

		effectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	private void ApplyModifiers(bool unapply = false)
	{
		var multiplier = unapply ? -1 : 1;

		foreach (ModifierEvaluatedData modifier in EffectEvaluatedData.ModifiersEvaluatedData)
		{
			switch (modifier.ModifierOperation)
			{
				case ModifierOperation.FlatBonus:
					modifier.Attribute.AddFlatModifier(multiplier * (int)modifier.Magnitude, modifier.Channel);
					break;

				case ModifierOperation.PercentBonus:
					modifier.Attribute.AddPercentModifier(multiplier * modifier.Magnitude, modifier.Channel);
					break;

				case ModifierOperation.Override:
					Validation.Assert(
						modifier.AttributeOverride is not null,
						"AttributeOverrideData should never be null at this point.");

					if (multiplier == 1)
					{
						modifier.Attribute.AddOverride(modifier.AttributeOverride.Value);
						break;
					}

					modifier.Attribute.ClearOverride(modifier.AttributeOverride.Value);
					break;
			}
		}
	}

	private void Execute()
	{
		EffectEvaluatedData effectEvaluatedData = EffectEvaluatedData;
		Effect.Execute(in effectEvaluatedData);
		ExecutionCount++;
	}

	private void UpdateEffectEvaluation()
	{
		if (!EffectData.DurationData.DurationMagnitude.HasValue)
		{
			ReapplyEffect(Effect);
			return;
		}

		var updatedDuration = EffectEvaluatedData.EvaluateDuration(EffectData.DurationData);

		if (EffectEvaluatedData.Duration > updatedDuration + Epsilon
			|| EffectEvaluatedData.Duration < updatedDuration - Epsilon)
		{
			RemainingDuration += updatedDuration - EffectEvaluatedData.Duration;
		}

		if (RemainingDuration <= 0
			&& EffectData.DurationData.DurationType == DurationType.HasDuration
			&& StackCount == 1)
		{
			Unapply();
			EffectEvaluatedData.Target.EffectsManager.RemoveActiveEffect_InternalCall(this);
			return;
		}

		ReapplyEffect(Effect);
	}

	private void Attribute_OnValueChanged(EntityAttribute attribute, int change)
	{
		UpdateEffectEvaluation();
	}

	private void Effect_OnLevelChanged(int obj)
	{
		UpdateEffectEvaluation();
	}

	private void Effect_OnSetByCallerFloatChanged(Tag identifierTag, float magnitude)
	{
		if (!_nonSnapshotSetByCallerTags.Contains(identifierTag))
		{
			return;
		}

		UpdateEffectEvaluation();
	}
}
