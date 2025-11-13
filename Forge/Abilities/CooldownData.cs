// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Contains information about an ability's cooldown state.
/// </summary>
/// <param name="CooldownTags">Tags associated with the cooldown.</param>
/// <param name="TotalTime">The total duration of the cooldown.</param>
/// <param name="RemainingTime">The remaining time left on the cooldown.</param>
public record struct CooldownData(TagContainer CooldownTags, float TotalTime, float RemainingTime);
