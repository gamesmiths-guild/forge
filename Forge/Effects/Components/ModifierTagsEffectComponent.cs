// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component for adding modifier tags to the target entity. Modifier tags will be removed once the
/// effects are removed.
/// </summary>
/// <remarks>
/// Only effects with duration can add modifier tags.
/// </remarks>
/// <param name="tagsToAdd">Which tags to be added as modifier tags.</param>
public class ModifierTagsEffectComponent(TagContainer tagsToAdd) : IEffectComponent
{
	private readonly TagContainer _tagsToAdd = tagsToAdd;

	/// <inheritdoc/>
	public bool OnActiveEffectAdded(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData)
	{
		target.Tags.AddModifierTags(_tagsToAdd);
		return true;
	}

	/// <inheritdoc/>
	public void OnActiveEffectUnapplied(
		IForgeEntity target,
		in ActiveEffectEvaluatedData activeEffectEvaluatedData,
		bool removed)
	{
		if (!removed)
		{
			return;
		}

		target.Tags.RemoveModifierTags(_tagsToAdd);
	}
}
