// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
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
public readonly record struct AbilityData
{
	/// <summary>
	/// Gets the name of the ability.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the effect that represents the cost of using the ability called when the ability is committed.
	/// </summary>
	public EffectData? CostEffect { get; }

	/// <summary>
	/// Gets a list of effects that represents the cooldowns of the ability.
	/// </summary>
	public EffectData[]? CooldownEffects { get; }

	/// <summary>
	/// Gets tags associated with the ability for categorization and filtering.
	/// </summary>
	public TagContainer? AbilityTags { get; }

	/// <summary>
	/// Gets the instancing policy for the ability, determining how instances are created and managed.
	/// </summary>
	public AbilityInstancingPolicy InstancingPolicy { get; }

	/// <summary>
	/// Gets a value indicating whether an instanced ability can be re-triggered while it is still active. If on, it
	/// will stop and re-trigger the ability.
	/// </summary>
	public bool RetriggerInstancedAbility { get; }

	/// <summary>
	/// Gets the trigger data associated with the ability, defining how and when the ability can be executed.
	/// </summary>
	public AbilityTriggerData? AbilityTriggerData { get; }

	/// <summary>
	/// Gets the tags that, if present on other abilities, will cause those abilities to be canceled when this ability
	/// is activated.
	/// </summary>
	public TagContainer? CancelAbilitiesWithTag { get; }

	/// <summary>
	/// Gets the tags that, if present on other abilities, will block those abilities from being activated while this
	/// ability is active.
	/// </summary>
	public TagContainer? BlockAbilitiesWithTag { get; }

	/// <summary>
	/// Gets tags that will be applied to the owner when the ability is activated.
	/// </summary>
	public TagContainer? ActivationOwnedTags { get; }

	/// <summary>
	/// Gets tags required on the owner to activate the ability.
	/// </summary>
	public TagContainer? ActivationRequiredTags { get; }

	/// <summary>
	/// Gets tags that, if present on the owner, will block the ability from being activated.
	/// </summary>
	public TagContainer? ActivationBlockedTags { get; }

	/// <summary>
	/// Gets tags required on the source to activate the ability.
	/// </summary>
	public TagContainer? SourceRequiredTags { get; }

	/// <summary>
	/// Gets tags that, if present on the source, will block the ability from being activated.
	/// </summary>
	public TagContainer? SourceBlockedTags { get; }

	/// <summary>
	/// Gets tags required on the target to activate the ability.
	/// </summary>
	public TagContainer? TargetRequiredTags { get; }

	/// <summary>
	/// Gets tags that, if present on the target, will block the ability from being activated.
	/// </summary>
	public TagContainer? TargetBlockedTags { get; }

	/// <summary>
	/// Gets the factory function to create custom ability behavior instances.
	/// </summary>
	public Func<IAbilityBehavior>? BehaviorFactory { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AbilityData"/> structure.
	/// </summary>
	/// <param name="name">The name of the ability.</param>
	/// <param name="costEffect">The effect that represents the cost of using the ability called when the ability is
	/// committed.</param>
	/// <param name="cooldownEffects">A list of effects that represents the cooldowns of the ability.</param>
	/// <param name="abilityTags">Tags associated with the ability for categorization and filtering.</param>
	/// <param name="instancingPolicy">The instancing policy for the ability, determining how instances are created and
	/// managed.</param>
	/// <param name="retriggerInstancedAbility">Flag indicating whether an instanced ability can be re-triggered while it is
	/// Still active. If on, it will stop and re-trigger the ability.</param>
	/// <param name="abilityTriggerData">The trigger data associated with the ability, defining how and when the ability can
	/// be executed.</param>
	/// <param name="cancelAbilitiesWithTag">Abilities with any of these tags will be canceled when this ability is
	/// executed.</param>
	/// <param name="blockAbilitiesWithTag">Abilities with any of these tags will be blocked from being executed while this
	/// ability is active.</param>
	/// <param name="activationOwnedTags">Tags that will be applied to the owner when the ability is activated.</param>
	/// <param name="activationRequiredTags">Tags required on the owner to activate the ability.</param>
	/// <param name="activationBlockedTags">Tags that, if present on the owner, will block the ability from being activated.
	/// </param>
	/// <param name="sourceRequiredTags">Tags required on the source to activate the ability.</param>
	/// <param name="sourceBlockedTags">Tags that, if present on the source, will block the ability from being activated.
	/// </param>
	/// <param name="targetRequiredTags">Tags required on the target to activate the ability.</param>
	/// <param name="targetBlockedTags">Tags that, if present on the target, will block the ability from being activated.
	/// </param>
	/// <param name="behaviorFactory">The factory function to create custom ability behavior instances.</param>
	public AbilityData(
		string name,
		EffectData? costEffect = null,
		EffectData[]? cooldownEffects = null,
		TagContainer? abilityTags = null,
		AbilityInstancingPolicy instancingPolicy = AbilityInstancingPolicy.PerEntity,
		bool retriggerInstancedAbility = false,
		AbilityTriggerData? abilityTriggerData = null,
		TagContainer? cancelAbilitiesWithTag = null,
		TagContainer? blockAbilitiesWithTag = null,
		TagContainer? activationOwnedTags = null,
		TagContainer? activationRequiredTags = null,
		TagContainer? activationBlockedTags = null,
		TagContainer? sourceRequiredTags = null,
		TagContainer? sourceBlockedTags = null,
		TagContainer? targetRequiredTags = null,
		TagContainer? targetBlockedTags = null,
		Func<IAbilityBehavior>? behaviorFactory = null)
	{
		Name = name;
		CostEffect = costEffect;
		CooldownEffects = cooldownEffects;
		AbilityTags = abilityTags;
		InstancingPolicy = instancingPolicy;
		RetriggerInstancedAbility = retriggerInstancedAbility;
		AbilityTriggerData = abilityTriggerData;
		CancelAbilitiesWithTag = cancelAbilitiesWithTag;
		BlockAbilitiesWithTag = blockAbilitiesWithTag;
		ActivationOwnedTags = activationOwnedTags;
		ActivationRequiredTags = activationRequiredTags;
		ActivationBlockedTags = activationBlockedTags;
		SourceRequiredTags = sourceRequiredTags;
		SourceBlockedTags = sourceBlockedTags;
		TargetRequiredTags = targetRequiredTags;
		TargetBlockedTags = targetBlockedTags;
		BehaviorFactory = behaviorFactory;

		if (Validation.Enabled)
		{
			ValidateData();
		}
	}

	private void ValidateData()
	{
		if (CooldownEffects is not null)
		{
			for (var i = 0; i < CooldownEffects.Length; i++)
			{
				Validation.Assert(
					Array.TrueForAll(
						CooldownEffects,
						x => x.DurationData.DurationType == Effects.Duration.DurationType.HasDuration),
					"Cooldown effects should have a duration.");

				Validation.Assert(
					Array.Exists(
						CooldownEffects[i].EffectComponents,
						x => x is ModifierTagsEffectComponent y && !y.TagsToAdd.IsEmpty),
					"Cooldown effects should have modifier tags.");
			}
		}

		if (CostEffect is not null)
		{
			Validation.Assert(
					CostEffect.Value.DurationData.DurationType == Effects.Duration.DurationType.Instant,
					"Cost effects should be instant.");
		}

		Validation.Assert(
			RetriggerInstancedAbility && InstancingPolicy == AbilityInstancingPolicy.PerEntity,
			"RetriggerInstancedAbility is only used when InstancingPolicy is PerEntity.");
	}
}
