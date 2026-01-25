// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Flags indicating the result of an ability activation attempt.
/// </summary>
/// <remarks>
/// This enumeration provides detailed outcomes for ability activation attempts, allowing the caller to determine the
/// specific reason for success or failure. Use this result to handle activation logic appropriately based on the
/// returned value.
/// </remarks>
[Flags]
public enum AbilityActivationFailures
{
	/// <summary>
	/// Successfully activated the ability.
	/// </summary>
	None = 0,

	/// <summary>
	/// Failed to activate the ability due to an invalid handler.
	/// </summary>
	InvalidHandler = 1 << 0,

	/// <summary>
	/// Failed to activate the ability because it is currently inhibited.
	/// </summary>
	Inhibited = 1 << 1,

	/// <summary>
	/// Failed to activate the ability because a persistent instance is already active.
	/// </summary>
	PersistentInstanceActive = 1 << 2,

	/// <summary>
	/// Failed to activate the ability because it is on cooldown.
	/// </summary>
	Cooldown = 1 << 3,

	/// <summary>
	/// Failed to activate the ability due to insufficient resources.
	/// </summary>
	InsufficientResources = 1 << 4,

	/// <summary>
	/// Failed to activate the ability due to unmet tag requirements.
	/// </summary>
	OwnerTagRequirements = 1 << 5,

	/// <summary>
	/// Failed to activate the ability due to unmet source tag requirements.
	/// </summary>
	SourceTagRequirements = 1 << 6,

	/// <summary>
	/// Failed to activate the ability due to unmet target tag requirements.
	/// </summary>
	TargetTagRequirements = 1 << 7,

	/// <summary>
	/// Failed to activate the ability due to being blocked by tags.
	/// </summary>
	BlockedByTags = 1 << 8,

	/// <summary>
	/// Failed to activate the ability because the target tag is not present.
	/// </summary>
	TargetTagNotPresent = 1 << 9,

	/// <summary>
	/// Failed to activate the ability due to invalid tag configuration.
	/// </summary>
	InvalidTagConfiguration = 1 << 11,
}
