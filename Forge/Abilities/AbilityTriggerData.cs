// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents data associated with an ability trigger, including the trigger's tag and source.
/// </summary>
/// <param name="TriggerTag">The tag identifying the specific trigger. This value is used to categorize or distinguish
/// the trigger.</param>
/// <param name="TriggerSource">The source of the trigger, indicating where or how the trigger originated.</param>
public readonly record struct AbilityTriggerData(
	Tag TriggerTag,
	AbitityTriggerSource TriggerSource);
