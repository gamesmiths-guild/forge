// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Attribute = Gamesmiths.Forge.Core.Attribute;

namespace Gamesmiths.Forge.GameplayEffects;

internal class ActiveGameplayEffect
{
	private const double Epsilon = 0.00001;

	private double _internalTime;

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

	internal void Apply(bool reApplication = false)
	{
		if (!reApplication)
		{
			ExecutionCount = 0;
			_internalTime = 0;
			RemainingDuration = GameplayEffectEvaluatedData.Duration;
		}

		if (!EffectData.SnapshopLevel)
		{
			GameplayEffect.OnLevelChanged += GameplayEffect_OnLevelChanged;
		}

		foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
		{
			if (!modifier.Snapshot && !reApplication)
			{
				Debug.Assert(
						modifier.BackingAttribute is not null,
						"All non-snapshots modifiers should have a BackingAttribute set.");

				modifier.BackingAttribute.OnValueChanged += Attribute_OnValueChanged;
			}
		}

		if (EffectData.PeriodicData.HasValue)
		{
			if (EffectData.PeriodicData.Value.ExecuteOnApplication &&
				!reApplication)
			{
				GameplayEffect.Execute(GameplayEffectEvaluatedData);
				ExecutionCount++;
			}

			if (!reApplication)
			{
				NextPeriodicTick = GameplayEffectEvaluatedData.Period;
			}
		}
		else
		{
			foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
			{
				switch (modifier.ModifierOperation)
				{
					case ModifierOperation.FlatBonus:
						modifier.Attribute.AddFlatModifier((int)modifier.Magnitude, modifier.Channel);
						break;

					case ModifierOperation.PercentBonus:
						modifier.Attribute.AddPercentModifier(modifier.Magnitude, modifier.Channel);
						break;

					case ModifierOperation.Override:
						modifier.Attribute.AddOverride((int)modifier.Magnitude, modifier.Channel);
						break;
				}
			}
		}
	}

	internal void Unapply(bool reApplication = false)
	{
		if (!EffectData.PeriodicData.HasValue)
		{
			foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
			{
				switch (modifier.ModifierOperation)
				{
					case ModifierOperation.FlatBonus:
						modifier.Attribute.AddFlatModifier(-(int)modifier.Magnitude, modifier.Channel);
						break;

					case ModifierOperation.PercentBonus:
						modifier.Attribute.AddPercentModifier(-modifier.Magnitude, modifier.Channel);
						break;

					case ModifierOperation.Override:
						modifier.Attribute.ClearOverride(modifier.Channel);
						break;
				}
			}
		}

		if (!reApplication)
		{
			StackCount = 0;

			foreach (ModifierEvaluatedData modifier in GameplayEffectEvaluatedData.ModifiersEvaluatedData)
			{
				if (!modifier.Snapshot)
				{
					Debug.Assert(
						modifier.BackingAttribute is not null,
						"All non-snapshots modifiers should have a BackingAttribute set.");

					modifier.BackingAttribute.OnValueChanged -= Attribute_OnValueChanged;
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
			ReEvaluateAndReApply(evaluatedGameplayEffect, evaluatedLevel);
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

		if (stackingData.ExecuteOnSuccessfulApplication == true)
		{
			GameplayEffect.Execute(GameplayEffectEvaluatedData);
			ExecutionCount++;
		}

		return true;
	}

	internal void RemoveStack()
	{
		var removed = StackCount == 1;

		GameplayEffectEvaluatedData.Target.EffectsManager.OnActiveGameplayEffectUnapplied_InternalCall(this, removed);

		if (removed)
		{
			Unapply();
			return;
		}

		StackCount--;
		ReEvaluateAndReApply(GameplayEffectEvaluatedData.GameplayEffect);
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
	}

	private void ExecutePeriodicEffects(double deltaTime)
	{
		_internalTime += deltaTime;

		if (EffectData.PeriodicData.HasValue)
		{
			while (_internalTime >= NextPeriodicTick - Epsilon)
			{
				GameplayEffect.Execute(GameplayEffectEvaluatedData);
				ExecutionCount++;
				NextPeriodicTick += GameplayEffectEvaluatedData.Period;
			}
		}
	}

	private void ReEvaluateAndReApply(GameplayEffect gameplayEffect, int? level = null)
	{
		Unapply(true);

		GameplayEffectEvaluatedData =
			new GameplayEffectEvaluatedData(
				gameplayEffect,
				GameplayEffectEvaluatedData.Target,
				StackCount,
				level);

		Apply(true);
	}

	private void Attribute_OnValueChanged(Attribute attribute, int change)
	{
		// This could be optimized by re-evaluating only the modifiers with the attribute that changed.
		ReEvaluateAndReApply(GameplayEffectEvaluatedData.GameplayEffect);
	}

	private void GameplayEffect_OnLevelChanged(int obj)
	{
		// This one has to re-calculate everything that uses ScalableFloats.
		ReEvaluateAndReApply(GameplayEffectEvaluatedData.GameplayEffect);
	}
}
