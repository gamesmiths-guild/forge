// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// A serieis of tag requirements to be evaluated and validated against a <see cref="TagContainer"/>.
/// </summary>
/// <param name="RequiredTags">The tags that the target container must have.</param>
/// <param name="IgnoreTags">The tags that the target container must not have.</param>
/// <param name="TagQuery">An optional tag query that must match.</param>
public readonly record struct TagRequirements(
	TagContainer? RequiredTags = null,
	TagContainer? IgnoreTags = null,
	TagQuery? TagQuery = null)
{
	/// <summary>
	/// Gets a value indicating whether this requirements have no required and ignored tags definied.
	/// </summary>
	public readonly bool IsEmpty => (RequiredTags?.IsEmpty != false)
		&& (IgnoreTags?.IsEmpty != false)
		&& (TagQuery?.IsEmpty != false);

	/// <summary>
	/// Validates if the target container meets the requirements.
	/// </summary>
	/// <param name="targetContainer">Target container to be checked against the requirements.</param>
	/// <returns><see langword="true"/> if the requirements are met; <see langword="false"/> otherwise.</returns>
	public bool RequirementsMet(in TagContainer targetContainer)
	{
		var hasRequired = RequiredTags is null || targetContainer.HasAll(RequiredTags);
		var hasIgnored = IgnoreTags is not null && targetContainer.HasAny(IgnoreTags);
		var matchQuery = TagQuery?.IsEmpty != false || TagQuery.Matches(targetContainer);

		return hasRequired && !hasIgnored && matchQuery;
	}
}
