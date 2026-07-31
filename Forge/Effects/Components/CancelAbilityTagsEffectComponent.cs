// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component for canceling the target's active abilities. Abilities carrying any of <paramref name="withTags"/>
/// and none of <paramref name="withoutTags"/> are canceled, either once on application or on each execution, according
/// to <paramref name="policy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each container is an independent filter: a <see langword="null"/> or empty container means "don't filter on this
/// side". Leaving both unset would therefore cancel every active ability, which for a component configured by tags is
/// a misconfiguration rather than an intent, so it cancels nothing instead and <see cref="EffectData"/> asserts
/// against it.
/// </para>
/// <para>
/// This component is stateless and is shared across all applications of its effect.
/// </para>
/// </remarks>
/// <param name="withTags">Abilities must have any of these tags to be canceled, or <see langword="null"/> to not
/// filter by required tags.</param>
/// <param name="withoutTags">Abilities having any of these tags are spared, or <see langword="null"/> to not filter by
/// blocking tags.</param>
/// <param name="policy">When the matching abilities are canceled.</param>
public class CancelAbilityTagsEffectComponent(
	TagContainer? withTags = null,
	TagContainer? withoutTags = null,
	CancelAbilityTagsPolicy policy = CancelAbilityTagsPolicy.OnApplication) : IEffectComponent
{
	internal TagContainer? WithTags { get; } = withTags;

	internal TagContainer? WithoutTags { get; } = withoutTags;

	internal CancelAbilityTagsPolicy Policy { get; } = policy;

	internal bool HasAnyFilter => WithTags?.IsEmpty == false || WithoutTags?.IsEmpty == false;

	/// <inheritdoc/>
	public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (Policy != CancelAbilityTagsPolicy.OnApplication)
		{
			return;
		}

		CancelMatchingAbilities(target);
	}

	/// <inheritdoc/>
	public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
	{
		if (Policy != CancelAbilityTagsPolicy.OnExecution)
		{
			return;
		}

		CancelMatchingAbilities(target);
	}

	private void CancelMatchingAbilities(IForgeEntity target)
	{
		// An unfiltered call would cancel every active ability. See the remarks on why that is treated as a no-op.
		if (!HasAnyFilter)
		{
			return;
		}

		target.Abilities.CancelAbilities(WithTags, WithoutTags);
	}
}
