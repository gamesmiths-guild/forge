// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Represents the data required to define an ability, including its name, effects, tags, and activation policies.
/// </summary>
/// <remarks>
/// This structure encapsulates all the metadata and configuration necessary to describe an ability in a system. It
/// includes optional effects for cost and cooldown, various tag-based requirements and restrictions, and policies for
/// instancing and retriggering abilities. Use this type to define the behavior and constraints of abilities in a
/// consistent and extensible manner.
/// </remarks>
/// <param name="Name">The name of the ability.</param>
/// <param name="CostEffect">The effect that represents the cost of using the ability called when the ability is
/// commited.</param>
/// <param name="CooldownEffect">The effect that represents the cooldown of the ability.</param>
/// <param name="AbilityTags">Tags associated with the ability for categorization and filtering.</param>
/// <param name="InstancingPolicy">The instancing policy for the ability, determining how instances are created and
/// managed.</param>
/// <param name="RetriggerInstancedAbility">Flag indicating whether an instanced ability can be re-triggered while it is
/// Still active. If on, it will stop and re-trigger the ability.</param>
/// <param name="AbilitTriggerData">The trigger data associated with the ability, defining how and when the ability can
/// be executed.</param>
/// <param name="CancelAbilitiesWithTag">Abilities with any of these tags will be canceled when this ability is
/// executed.</param>
/// <param name="BlockAbilitiesWithTag">Abilities with any of these tags will be blocked from being executed while this
/// ability is active.</param>
/// <param name="ActivationOwnedTags">Tags that will be applied to the owner when the ability is activated.</param>
/// <param name="ActivationRequiredTags">Tags required on the owner to activate the ability.</param>
/// <param name="ActivationBlockedTags">Tags that, if present on the owner, will block the ability from being activated.
/// </param>
/// <param name="SourceRequiredTags">Tags required on the source to activate the ability.</param>
/// <param name="SourceBlockedTags">Tags that, if present on the source, will block the ability from being activated.
/// </param>
/// <param name="TargetRequiredTags">Tags required on the target to activate the ability.</param>
/// <param name="TargetBlockedTags">Tags that, if present on the target, will block the ability from being activated.
/// </param>
public readonly record struct AbilityData(
	string Name,
	EffectData? CostEffect = null,
	EffectData? CooldownEffect = null,
	TagContainer? AbilityTags = null,
	AbilityInstancingPolicy InstancingPolicy = AbilityInstancingPolicy.PerEntity,
	bool RetriggerInstancedAbility = false,
	AbilitTriggerData? AbilitTriggerData = null,
	TagContainer? CancelAbilitiesWithTag = null,
	TagContainer? BlockAbilitiesWithTag = null,
	TagContainer? ActivationOwnedTags = null,
	TagContainer? ActivationRequiredTags = null,
	TagContainer? ActivationBlockedTags = null,
	TagContainer? SourceRequiredTags = null,
	TagContainer? SourceBlockedTags = null,
	TagContainer? TargetRequiredTags = null,
	TagContainer? TargetBlockedTags = null);
