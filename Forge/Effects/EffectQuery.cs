// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// A filter that selects effects by their identity, the tags they grant, their source, or what they modify.
/// </summary>
/// <remarks>
/// <para>
/// Every field is optional and all defined fields are combined with AND, in the same shape as
/// <see cref="TagRequirements"/>. An empty query matches every effect, so
/// <see cref="EffectsManager.GetActiveEffects(EffectQuery)"/> with a <see langword="default"/> query is equivalent to
/// <see cref="EffectsManager.GetActiveEffects()"/>.
/// </para>
/// <para>
/// Tag queries are evaluated against tag containers the effect may not have at all. An effect carrying no
/// <see cref="EffectData.EffectTags"/> is matched as if it carried an empty container, so a negative query such as
/// <c>NoTagsMatch(effect.curse)</c> still matches it.
/// </para>
/// </remarks>
/// <param name="EffectDefinition">The exact <see cref="EffectData"/> the effect must have been built from.</param>
/// <param name="EffectTagQuery">A query matched against <see cref="EffectData.EffectTags"/>.</param>
/// <param name="GrantedTagQuery">A query matched against the tags the effect grants to its target through
/// <see cref="ModifierTagsEffectComponent"/>.</param>
/// <param name="OwningTagQuery">A query matched against the union of the effect's own tags and the tags it grants.
/// </param>
/// <param name="SourceTagRequirements">Requirements matched against the tags of the entity that applied the effect.
/// </param>
/// <param name="ModifyingAttribute">An attribute that at least one of the effect's modifiers must target.</param>
/// <param name="EffectSource">The entity that must have applied the effect.</param>
/// <param name="CustomMatch">An arbitrary predicate, for anything the other fields cannot express.</param>
public readonly record struct EffectQuery(
	EffectData? EffectDefinition = null,
	TagQuery? EffectTagQuery = null,
	TagQuery? GrantedTagQuery = null,
	TagQuery? OwningTagQuery = null,
	TagRequirements? SourceTagRequirements = null,
	StringKey? ModifyingAttribute = null,
	IForgeEntity? EffectSource = null,
	Func<Effect, bool>? CustomMatch = null)
{
	/// <summary>
	/// Gets a value indicating whether this query defines no filters at all, and therefore matches every effect.
	/// </summary>
	public bool IsEmpty => !EffectDefinition.HasValue
		&& (EffectTagQuery?.IsEmpty != false)
		&& (GrantedTagQuery?.IsEmpty != false)
		&& (OwningTagQuery?.IsEmpty != false)
		&& (SourceTagRequirements?.IsEmpty != false)
		&& !ModifyingAttribute.HasValue
		&& EffectSource is null
		&& CustomMatch is null;

	/// <summary>
	/// Checks whether the given effect satisfies every filter defined by this query.
	/// </summary>
	/// <param name="effect">The effect to be tested against this query, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the effect matches; <see langword="false"/> otherwise.
	/// A <see langword="null"/> effect never matches.</returns>
	public bool Matches(Effect? effect)
	{
		if (effect is null)
		{
			return false;
		}

		if (EffectDefinition.HasValue && effect.EffectData != EffectDefinition.Value)
		{
			return false;
		}

		if (EffectSource is not null && !ReferenceEquals(effect.Ownership.Source, EffectSource))
		{
			return false;
		}

		if (EffectTagQuery?.IsEmpty == false
			&& !effect.MatchesTagQuery(EffectTagQuery, effect.EffectData.EffectTags))
		{
			return false;
		}

		if (GrantedTagQuery?.IsEmpty == false
			&& !effect.MatchesTagQuery(GrantedTagQuery, effect.CachedGrantedTags))
		{
			return false;
		}

		if (OwningTagQuery?.IsEmpty == false
			&& !effect.MatchesTagQuery(OwningTagQuery, effect.GetOwningTags()))
		{
			return false;
		}

		if (SourceTagRequirements?.IsEmpty == false && !MatchesSourceRequirements(effect))
		{
			return false;
		}

		if (ModifyingAttribute.HasValue && !ModifiesAttribute(effect, ModifyingAttribute.Value))
		{
			return false;
		}

		return CustomMatch is null || CustomMatch(effect);
	}

	/// <summary>
	/// Checks whether the effect behind the given handle satisfies every filter defined by this query.
	/// </summary>
	/// <param name="handle">The handle of the active effect to be tested against this query, or
	/// <see langword="null"/>.</param>
	/// <param name="ignoredHandles">Handles that never match, regardless of the query. Used by components that must
	/// exclude their own effect from the results.</param>
	/// <returns><see langword="true"/> if the active effect matches; <see langword="false"/> otherwise.
	/// <see langword="null"/> and invalid handles never match.</returns>
	public bool Matches(
		ActiveEffectHandle? handle,
		IReadOnlyCollection<ActiveEffectHandle>? ignoredHandles = null)
	{
		if (handle?.Effect is null)
		{
			return false;
		}

		if (ignoredHandles?.Contains(handle) == true)
		{
			return false;
		}

		return Matches(handle.Effect);
	}

	private static bool ModifiesAttribute(Effect effect, StringKey attribute)
	{
		return Array.Exists(effect.EffectData.Modifiers, x => x.Attribute == attribute);
	}

	private bool MatchesSourceRequirements(Effect effect)
	{
		TagContainer? sourceTags = effect.Ownership.Source?.Tags.AllTags;

		if (sourceTags is null)
		{
			TagsManager? tagsManager = effect.ResolveTagsManager();

			if (tagsManager is null)
			{
				return false;
			}

			// Keeps the semantics honest: a missing source cannot satisfy required tags, but trivially satisfies
			// ignored ones.
			sourceTags = new TagContainer(tagsManager);
		}

		return SourceTagRequirements!.Value.RequirementsMet(sourceTags);
	}
}
