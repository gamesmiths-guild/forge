// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// Represents an active gameplay effect that is currently affecting an entity.
/// </summary>
internal class ActiveGameplayEffect
{
	private const double Epsilon = 0.00001;

	private double _internalTime;

	private bool _isInhibited;

	internal ActiveGameplayEffectHandle Handle { get; }

	internal GameplayEffectEvaluatedData GameplayEffectEvaluatedData { get; private set; }

	internal double RemainingDuration { get; set; }

	internal double NextPeriodicTick { get; private set; }

	internal int ExecutionCount { get; private set; }

	internal int StackCount { get; private set; }

	internal bool IsExpired => EffectData.DurationData.Type ==
		DurationType.HasDuration &&
		RemainingDuration <= 0;

	internal GameplayEffectData EffectData => GameplayEffectEvaluatedData.GameplayEffect.EffectData;

	internal GameplayEffect GameplayEffect => GameplayEffectEvaluatedData.GameplayEffect;

	internal ActiveGameplayEffect(GameplayEffect gameplayEffect, IForgeEntity target)
	{
		Handle = new ActiveGameplayEffectHandle(this);

		if (gameplayEffect.EffectData.StackingData.HasValue)
		{
			StackCount = gameplayEffect.EffectData.StackingData.Value.InitialStack.GetValue(
				GameplayEffectEvaluatedData.Level);
		}
		else
		{
			StackCount = 1;
		}

		GameplayEffectEvaluatedData = new GameplayEffectEvaluatedData(gameplayEffect, target, StackCount);
	}

	internal void Apply(bool reApplication = false, bool inhibited = false)
	{
		if (!reApplication)
		{
			ExecutionCount = 0;
			_internalTime = 0;
			_isInhibited = inhibited;
			RemainingDuration = GameplayEffectEvaluatedData.Duration;

			if (!EffectData.SnapshopLevel)
			{
				GameplayEffect.OnLevelChanged += GameplayEffect_OnLevelChanged;
			}

			// Maybe save this in a private field? TrackedAttributes.
			var attributesToSubscribe = new HashSet<Attribute>();

			foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
			{
				if (!modifier.Snapshot)
				{
					foreach (Attribute attribute in modifier.BackingAttributes)
					{
						attributesToSubscribe.Add(attribute);
					}
				}
			}

			foreach (Attribute attribute in attributesToSubscribe)
			{
				attribute.OnValueChanged += Attribute_OnValueChanged;
			}
		}

		if (EffectData.PeriodicData.HasValue)
		{
			if (EffectData.PeriodicData.Value.ExecuteOnApplication &&
				!reApplication && !_isInhibited)
			{
				Execute();
			}

			if (!reApplication)
			{
				NextPeriodicTick = GameplayEffectEvaluatedData.Period;
			}
		}
		else if (!_isInhibited)
		{
			ApplyModifiers();
		}
	}

	internal void Unapply(bool reApplication = false)
	{
		if (!EffectData.PeriodicData.HasValue && !_isInhibited)
		{
			ApplyModifiers(true);
		}

		if (!reApplication)
		{
			StackCount = 0;

			foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
			{
				if (!modifier.Snapshot)
				{
					foreach (Attribute attribute in modifier.BackingAttributes)
					{
						attribute.OnValueChanged -= Attribute_OnValueChanged;
					}
				}
			}

			if (!EffectData.SnapshopLevel)
			{
				GameplayEffect.OnLevelChanged -= GameplayEffect_OnLevelChanged;
			}
		}
	}

	internal bool AddStack(GameplayEffect gameplayEffect, int stacks = 1)
	{
		Debug.Assert(
			EffectData.StackingData.HasValue,
			"StackingData should never be null at this point.");

		Debug.Assert(
			stacks > 0,
			"Number of stacks should be higher than 1.");

		var hasChanges = false;
		var resetStacks = false;

		StackingData stackingData = EffectData.StackingData.Value;
		var evaluatedLevel = GameplayEffectEvaluatedData.Level;

		// We have to evaluate level before checking the stack count since the level could change.
		if (stackingData.LevelDenialPolicy.HasValue)
		{
			Debug.Assert(
				stackingData.LevelOverridePolicy.HasValue,
				"LevelOverridePolicy should never be null at this point.");

			// Determine the relationship.
			LevelComparison relation;
			if (gameplayEffect.Level > evaluatedLevel)
			{
				relation = LevelComparison.Higher;
			}
			else if (gameplayEffect.Level < evaluatedLevel)
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
				Debug.Assert(
					stackingData.LevelOverrideStackCountPolicy.HasValue,
					"LevelOverrideStackCountPolicy should never be null at this point.");

				evaluatedLevel = gameplayEffect.Level;
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

		GameplayEffect evaluatedGameplayEffect = GameplayEffectEvaluatedData.GameplayEffect;

		if (stackingData.OwnerDenialPolicy.HasValue)
		{
			if (stackingData.OwnerDenialPolicy.Value == StackOwnerDenialPolicy.DenyIfDifferent &&
				GameplayEffectEvaluatedData.GameplayEffect.Ownership.Owner != gameplayEffect.Ownership.Owner)
			{
				return false;
			}

			if (stackingData.OwnerOverridePolicy == StackOwnerOverridePolicy.Override &&
				GameplayEffectEvaluatedData.GameplayEffect.Ownership.Owner != gameplayEffect.Ownership.Owner)
			{
				evaluatedGameplayEffect = gameplayEffect;
				hasChanges = true;

				Debug.Assert(
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

		// It can be a successfull application and still not increase stack count.
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
			ReapplyEffect(evaluatedGameplayEffect, evaluatedLevel, true);
		}
		else
		{
			GameplayEffectEvaluatedData effectEvaluatedData = GameplayEffectEvaluatedData;
			effectEvaluatedData.Target.EffectsManager.TriggerCuesUpdate_InternalCall(in effectEvaluatedData);
		}

		if (stackingData.ApplicationRefreshPolicy == StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication)
		{
			RemainingDuration = GameplayEffectEvaluatedData.Duration;
		}

		if (stackingData.ApplicationResetPeriodPolicy == StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication)
		{
			_internalTime = 0;
			NextPeriodicTick = GameplayEffectEvaluatedData.Period;
		}

		if (stackingData.ExecuteOnSuccessfulApplication == true && !_isInhibited)
		{
			Execute();
		}

		return true;
	}

	internal void RemoveStack()
	{
		var removed = StackCount == 1;

		GameplayEffectEvaluatedData.Target.EffectsManager.OnActiveGameplayEffectUnapplied_InternalCall(this);

		if (removed)
		{
			Unapply();
			return;
		}

		StackCount--;
		ReapplyEffect(GameplayEffectEvaluatedData.GameplayEffect, isStackingCall: true);
	}

	internal void Update(double deltaTime)
	{
		if (EffectData.DurationData.Type == DurationType.HasDuration)
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
							var periodicDelta = Math.Min(-RemainingDuration, GameplayEffectEvaluatedData.Duration);
							ExecutePeriodicEffects(periodicDelta);
							RemainingDuration += GameplayEffectEvaluatedData.Duration;
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

		GameplayEffectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	internal void SetInhibit(bool value)
	{
		if (_isInhibited == value)
		{
			return;
		}

		_isInhibited = value;

		if (EffectData.PeriodicData.HasValue)
		{
			if (_isInhibited)
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
				NextPeriodicTick = _internalTime + GameplayEffectEvaluatedData.Period;
			}

			return;
		}

		ApplyModifiers(_isInhibited);
	}

	private void ExecutePeriodicEffects(double deltaTime)
	{
		_internalTime += deltaTime;

		if (EffectData.PeriodicData.HasValue)
		{
			while (_internalTime >= NextPeriodicTick - Epsilon)
			{
				if (!_isInhibited)
				{
					Execute();
				}

				NextPeriodicTick += GameplayEffectEvaluatedData.Period;
			}
		}
	}

	private void ReapplyEffect(GameplayEffect gameplayEffect, int? level = null, bool isStackingCall = false)
	{
		Unapply(true);

		GameplayEffectEvaluatedData =
			new GameplayEffectEvaluatedData(
				gameplayEffect,
				GameplayEffectEvaluatedData.Target,
				StackCount,
				level);

		Apply(reApplication: true);

		GameplayEffectEvaluatedData.Target.EffectsManager.OnActiveGameplayEffectChanged_InternalCall(this);

		GameplayEffectEvaluatedData effectEvaluatedData = GameplayEffectEvaluatedData;

		if (!GameplayEffectEvaluatedData.GameplayEffect.EffectData.SuppressStackingCues || !isStackingCall)
		{
			GameplayEffectEvaluatedData.Target.EffectsManager.TriggerCuesUpdate_InternalCall(in effectEvaluatedData);
		}

		effectEvaluatedData.Target.Attributes.ApplyPendingValueChanges();
	}

	private void ApplyModifiers(bool unapply = false)
	{
		var multiplier = unapply ? -1 : 1;

		foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
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
					Debug.Assert(
						modifier.AttributeOverride is not null,
						"AttributeOverrideData should never be null at this point.");

					if (multiplier == 1)
					{
						modifier.Attribute.AddOverride(modifier.AttributeOverride);
						break;
					}

					modifier.Attribute.ClearOverride(modifier.AttributeOverride);
					break;
			}
		}
	}

	private void Execute()
	{
		GameplayEffectEvaluatedData effectEvaluatedData = GameplayEffectEvaluatedData;
		GameplayEffect.Execute(in effectEvaluatedData);
		ExecutionCount++;
	}

	private void Attribute_OnValueChanged(Attribute attribute, int change)
	{
		// This could be optimized by re-evaluating only the modifiers with the attribute that changed.
		ReapplyEffect(GameplayEffectEvaluatedData.GameplayEffect);
	}

	private void GameplayEffect_OnLevelChanged(int obj)
	{
		// This one has to re-calculate everything that uses ScalableFloats.
		ReapplyEffect(GameplayEffectEvaluatedData.GameplayEffect);
	}
}
