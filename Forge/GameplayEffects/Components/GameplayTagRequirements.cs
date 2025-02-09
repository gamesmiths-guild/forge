// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.GameplayEffects.Components;

/// <summary>
/// A serieis of tag requirements to be evaluated and validated against a <see cref="GameplayTagContainer"/>.
/// </summary>
/// <param name="requiredTags">The tags that the target container must have.</param>
/// <param name="ignoreTags">The tags that the target container must not have.</param>
/// <param name="tagQuery">An optional tag query that must match.</param>
public readonly struct GameplayTagRequirements(
	GameplayTagContainer requiredTags,
	GameplayTagContainer ignoreTags,
	GameplayTagQuery tagQuery)
{
	/// <summary>
	/// Gets the set of required tags for this requirements.
	/// </summary>
	public GameplayTagContainer RequireTags { get; } = requiredTags;

	/// <summary>
	/// Gets the set of ignored tags for this requirements.
	/// </summary>
	public GameplayTagContainer IgnoreTags { get; } = ignoreTags;

	/// <summary>
	/// Gets the tag query that this requirements must match.
	/// </summary>
	public GameplayTagQuery TagQuery { get; } = tagQuery;

	/// <summary>
	/// Gets a value indicating whether this requirements have no required and ignored tags definied.
	/// </summary>
	public readonly bool IsEmpty => RequireTags.IsEmpty && IgnoreTags.IsEmpty && TagQuery.IsEmpty;

	/// <summary>
	/// Validates if the target container meets the requirements.
	/// </summary>
	/// <param name="targetContainer">Target container to be checked against the requirements.</param>
	/// <returns><see langword="true"/> if the requirements are met; <see langword="false"/> otherwise.</returns>
	public bool RequirementsMet(in GameplayTagContainer targetContainer)
	{
		var hasRequired = targetContainer.HasAll(RequireTags);
		var hasIgnored = targetContainer.HasAny(IgnoreTags);
		var matchQuery = TagQuery.IsEmpty || TagQuery.Matches(targetContainer);

		return hasRequired && !hasIgnored && matchQuery;
	}
}
