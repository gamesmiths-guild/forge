// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// A serieis of tag requirements to be evaluated and validated against a <see cref="TagContainer"/>.
/// </summary>
/// <param name="requiredTags">The tags that the target container must have.</param>
/// <param name="ignoreTags">The tags that the target container must not have.</param>
/// <param name="tagQuery">An optional tag query that must match.</param>
public readonly struct TagRequirements(
	TagContainer? requiredTags = null,
	TagContainer? ignoreTags = null,
	TagQuery? tagQuery = null)
{
	/// <summary>
	/// Gets the set of required tags for this requirements.
	/// </summary>
	public TagContainer? RequireTags { get; } = requiredTags;

	/// <summary>
	/// Gets the set of ignored tags for this requirements.
	/// </summary>
	public TagContainer? IgnoreTags { get; } = ignoreTags;

	/// <summary>
	/// Gets the tag query that this requirements must match.
	/// </summary>
	public TagQuery? TagQuery { get; } = tagQuery;

	/// <summary>
	/// Gets a value indicating whether this requirements have no required and ignored tags definied.
	/// </summary>
	public readonly bool IsEmpty => (RequireTags?.IsEmpty != false)
		&& (IgnoreTags?.IsEmpty != false)
		&& (TagQuery?.IsEmpty != false);

	/// <summary>
	/// Validates if the target container meets the requirements.
	/// </summary>
	/// <param name="targetContainer">Target container to be checked against the requirements.</param>
	/// <returns><see langword="true"/> if the requirements are met; <see langword="false"/> otherwise.</returns>
	public bool RequirementsMet(in TagContainer targetContainer)
	{
		var hasRequired = RequireTags is null || targetContainer.HasAll(RequireTags);
		var hasIgnored = IgnoreTags is not null && targetContainer.HasAny(IgnoreTags);
		var matchQuery = TagQuery?.IsEmpty != false || TagQuery.Matches(targetContainer);

		return hasRequired && !hasIgnored && matchQuery;
	}
}
