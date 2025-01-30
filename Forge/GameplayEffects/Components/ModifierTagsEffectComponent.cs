// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.GameplayEffects.Components;

/// <summary>
/// Gameplay effect component for adding modifier tags to the target entity. Modifier tags will be removed once the
/// effects are removed.
/// </summary>
/// <remarks>
/// Only effects with duration can add modifier tags.
/// </remarks>
/// <param name="tagsToAdd">Which tags to be added as modifier tags.</param>
public class ModifierTagsEffectComponent(GameplayTagContainer tagsToAdd) : IGameplayEffectComponent
{
	private readonly GameplayTagContainer _tagsToAdd = tagsToAdd;

	/// <inheritdoc/>
	public void OnActiveGameplayEffectAdded(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		target.GameplayTags.AddModifierTags(_tagsToAdd);
	}

	/// <inheritdoc/>
	public void OnActiveGameplayEffectRemoved(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		target.GameplayTags.RemoveModifierTags(_tagsToAdd);
	}
}
