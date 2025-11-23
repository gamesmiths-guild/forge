// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents the result of an attempt to activate an ability.
/// </summary>
/// <remarks>
/// This enumeration provides detailed outcomes for ability activation attempts, allowing the caller to determine the
/// specific reason for success or failure. Use this result to handle activation logic appropriately based on the
/// returned value.
/// </remarks>
public enum AbilityActivationResult
{
	/// <summary>
	/// Successfully activated the ability.
	/// </summary>
	Success = 0,

	/// <summary>
	/// Failed to activate the ability due to an invalid handler.
	/// </summary>
	FailedInvalidHandler = 1,

	/// <summary>
	/// Failed to activate the ability because it is currently inhibited.
	/// </summary>
	FailedInhibition = 2,

	/// <summary>
	/// Failed to activate the ability because a persistent instance is already active.
	/// </summary>
	FailedPersistentInstanceActive = 3,

	/// <summary>
	/// Failed to activate the ability because it is on cooldown.
	/// </summary>
	FailedCooldown = 4,

	/// <summary>
	/// Failed to activate the ability due to insufficient resources.
	/// </summary>
	FailedInsufficientResources = 5,

	/// <summary>
	/// Failed to activate the ability due to unmet tag requirements.
	/// </summary>
	FailedOwnerTagRequirements = 6,

	/// <summary>
	/// Failed to activate the ability due to unmet source tag requirements.
	/// </summary>
	FailedSourceTagRequirements = 7,

	/// <summary>
	/// Failed to activate the ability due to unmet target tag requirements.
	/// </summary>
	FailedTargetTagRequirements = 8,

	/// <summary>
	/// Failed to activate the ability due to being blocked by tags.
	/// </summary>
	FailedBlockedByTags = 9,

	/// <summary>
	/// Failed to activate the ability because the target tag is not present.
	/// </summary>
	FailedTargetTagNotPresent = 10,

	/// <summary>
	/// Failed to activate the ability due to invalid tag configuration.
	/// </summary>
	FailedInvalidTagConfiguration = 11,
}
