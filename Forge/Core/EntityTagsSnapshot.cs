// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Core;

// An entity's tags as an entity snapshot took them: the base tags and how many times each modifier tag was added.
internal sealed record EntityTagsSnapshot(Tag[] BaseTags, KeyValuePair<Tag, int>[] ModifierTagCounts);
