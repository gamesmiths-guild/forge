// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Specifies the source or condition that triggers an ability.
/// </summary>
/// <remarks>
/// This enumeration defines the possible ways an ability can be triggered, such as through explicit events or changes
/// in the entity's state (e.g., the addition or presence of specific tags).
/// </remarks>
public enum AbitityTriggerSource : byte
{
	/// <summary>
	/// Ability is triggered by an explicit event call.
	/// </summary>
	Event = 0,

	/// <summary>
	/// Ability is triggered when a specific tag is added to the entity.
	/// </summary>
	TagAdded = 1,

	/// <summary>
	/// Ability is triggered when a specific tag is added on the entity and removed when the tag is gone.
	/// </summary>
	TagPresent = 2,
}
