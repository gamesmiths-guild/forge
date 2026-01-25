// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents a single activation/execution of an Ability.
/// Responsible for per-activation state (activation-owned tags, blocking tags).
/// </summary>
internal sealed class AbilityInstance
{
	private readonly Ability _ability;

	internal bool IsActive { get; private set; }

	internal IForgeEntity? Target { get; }

	internal AbilityInstanceHandle Handle { get; }

	internal AbilityInstance(Ability ability, IForgeEntity? target)
	{
		_ability = ability;
		Target = target;
		Handle = new AbilityInstanceHandle(this);
	}

	internal void Start(float magnitude = 0f)
	{
		if (IsActive)
		{
			return;
		}

		ApplyActivationState();
		IsActive = true;
		_ability.OnInstanceStarted(this, magnitude);
	}

	internal void Start<TData>(TData data, float magnitude = 0f)
	{
		if (IsActive)
		{
			return;
		}

		ApplyActivationState();
		IsActive = true;
		_ability.OnInstanceStarted(this, data, magnitude);
	}

	internal void End()
	{
		if (!IsActive)
		{
			return;
		}

		// Remove activation-owned tags.
		if (_ability.AbilityData.ActivationOwnedTags is not null)
		{
			_ability.Owner.Tags.RemoveModifierTags(_ability.AbilityData.ActivationOwnedTags);
		}

		// Unblock abilities with tags for this instance.
		TagContainer? blockTags = _ability.AbilityData.BlockAbilitiesWithTag;
		if (blockTags is not null)
		{
			_ability.Owner.Abilities.BlockedAbilityTags.RemoveModifierTags(blockTags);
		}

		IsActive = false;
		_ability.OnInstanceEnded(this);
	}

	internal void Cancel()
	{
		End();
	}

	private void ApplyActivationState()
	{
		// Apply activation-owned tags.
		if (_ability.AbilityData.ActivationOwnedTags is not null)
		{
			_ability.Owner.Tags.AddModifierTags(_ability.AbilityData.ActivationOwnedTags);
		}

		// Block abilities with tags while this instance is active.
		TagContainer? blockTags = _ability.AbilityData.BlockAbilitiesWithTag;
		if (blockTags is not null)
		{
			_ability.Owner.Abilities.BlockedAbilityTags.AddModifierTags(blockTags);
		}
	}
}
