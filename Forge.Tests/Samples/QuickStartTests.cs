// Copyright © Gamesmiths Guild.

#pragma warning disable

using System.Buffers.Text;
using FluentAssertions;
using Gamesmiths;
using Gamesmiths.Forge;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests;
using Gamesmiths.Forge.Tests.Core;
using static Gamesmiths.Forge.Tests.Samples.QuickStartTests;

namespace Gamesmiths.Forge.Tests.Samples;

public class QuickStartTests(ExamplesTestFixture tagsAndCueFixture) : IClassFixture<ExamplesTestFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCueFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCueFixture.CuesManager;

	[Fact]
	[Trait("Quick Start", null)]
	public void Creating_a_basic_entity()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(100);
		player.Attributes["PlayerAttributeSet.Mana"].CurrentValue.Should().Be(100);
		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(10);
		player.Attributes["PlayerAttributeSet.Speed"].CurrentValue.Should().Be(5);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Working_with_tags()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Tags must be requested through the static method Tag.RequestTag
		var playerTag = Tag.RequestTag(tagsManager, "character.player");
		var warriorTag = Tag.RequestTag(tagsManager, "class.warrior");

		// Check if the player has specific tags
		var isPlayer = player.Tags.CombinedTags.HasTag(playerTag);
		var isWarrior = player.Tags.CombinedTags.HasTag(warriorTag);

		isPlayer.Should().BeTrue();
		isWarrior.Should().BeTrue();
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Instant_effect_example()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a damage effect data
		var damageEffectData = new EffectData(
			"Basic Attack",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-10) // -10 damage
					)
				)
			});

		// Create an effect instance and apply the effect
		var damageEffect = new Effect(damageEffectData, new EffectOwnership(player, player));
		player.EffectsManager.ApplyEffect(damageEffect);

		// Check the current health value
		int currentHealth = player.Attributes["PlayerAttributeSet.Health"].CurrentValue;
		int baseHealth = player.Attributes["PlayerAttributeSet.Health"].BaseValue;

		// Assuming initial health was 100
		currentHealth.Should().Be(90);
		baseHealth.Should().Be(90);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Buff_effect_with_duration()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a strength buff effect that lasts for 10 seconds
		var strengthBuffEffectData = new EffectData(
			"Strength Potion",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10.0f))), // 10 seconds duration
			new[] {
				new Modifier(
					"PlayerAttributeSet.Strength",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(5) // +5 strength
					)
				)
			}
		);

		// Create and apply the effect
		var strengthBuffEffect = new Effect(strengthBuffEffectData, new EffectOwnership(player, player));

		ActiveEffectHandle? buffHandle = player.EffectsManager.ApplyEffect(strengthBuffEffect);

		// Check strength values
		int buffedStrength = player.Attributes["PlayerAttributeSet.Strength"].CurrentValue;
		int baseStrength = player.Attributes["PlayerAttributeSet.Strength"].BaseValue;
		int strengthModifier = player.Attributes["PlayerAttributeSet.Strength"].ValidModifier;

		buffHandle.Should().NotBeNull();
		buffedStrength.Should().Be(15); // Assuming base strength was 10
		baseStrength.Should().Be(10); // Base value remains unchanged
		strengthModifier.Should().Be(5); // +5 from the buff
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Updating_effects()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		player.EffectsManager.UpdateEffects(0.016f);
		player.EffectsManager.UpdateEffects(1.0f);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Infinite_effect_example_equipment_buff()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create an infinite effect for a piece of equipment
		var equipmentBuffEffectData = new EffectData(
			"Magic Sword Bonus",
			new DurationData(DurationType.Infinite),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Strength",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(10) // +10 Strength
					)
				)
			}
		);

		// Apply the equipment buff
		var equipmentBuffEffect = new Effect(equipmentBuffEffectData, new EffectOwnership(player, player));
		var activeEffectHandle = player.EffectsManager.ApplyEffect(equipmentBuffEffect);

		// Assuming base strength was 10
		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(20);
		// +10 from buff
		player.Attributes["PlayerAttributeSet.Strength"].ValidModifier.Should().Be(10);

		// Remove the effect manually (e.g., when the item is unequipped)
		if (activeEffectHandle is not null)
		{
			player.EffectsManager.RemoveEffect(activeEffectHandle);
		}

		// Assuming base strength was 10
		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(10);
		// 0 because the effect was removed
		player.Attributes["PlayerAttributeSet.Strength"].ValidModifier.Should().Be(0);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Periodic_effect_example()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a poison effect that ticks every 2 seconds for 10 seconds
		var poisonEffectData = new EffectData(
			"Poison",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10.0f))),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-5) // -5 damage per tick
					)
				)
			},
			periodicData: new PeriodicData(
				Period: new ScalableFloat(2.0f),
				ExecuteOnApplication: true,
				PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			)
		);

		// Apply the poison effect
		var poisonEffect = new Effect(poisonEffectData, new EffectOwnership(player, player));
		player.EffectsManager.ApplyEffect(poisonEffect);

		player.EffectsManager.UpdateEffects(10f); // Simulate 10 seconds of game time

		// Assuming initial health was 100, 5 damage every 2 seconds for 10 seconds (5 ticks) + 1 initial tick
		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(70);
		player.Attributes["PlayerAttributeSet.Health"].BaseValue.Should().Be(70);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Stacking_poison_effect_example()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a poison effect that stacks up to 3 times
		var stackingPoisonEffectData = new EffectData(
			"Stacking Poison",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(6.0f))), // Each stack lasts 6 seconds
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-3) // -3 damage per stack
					)
				)
			},
			periodicData: new PeriodicData(
				Period: new ScalableFloat(2.0f),
				ExecuteOnApplication: false,
				PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			),
			stackingData: new StackingData(
				StackLimit: new ScalableInt(3), // Max 3 stacks
				InitialStack: new ScalableInt(1), // Starts with 1 stack
				OverflowPolicy: StackOverflowPolicy.DenyApplication, // Deny if max stacks reached
				MagnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
				ExpirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				StackPolicy: StackPolicy.AggregateBySource, // Aggregate stacks from the same source
				StackLevelPolicy: StackLevelPolicy.SegregateLevels, // Each stack can have its own level

				// The next two values must be defined because this is a periodic effect with stacking
				ExecuteOnSuccessfulApplication: false, // Do not execute on successful application
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication
			)
		);

		// Apply the stacking poison effect multiple times to demonstrate stacking
		var stackingPoisonEffect = new Effect(stackingPoisonEffectData, new EffectOwnership(player, player));

		// Apply the effect three times, each time adds a stack for a total of -9 damage per tick
		player.EffectsManager.ApplyEffect(stackingPoisonEffect);
		player.EffectsManager.ApplyEffect(stackingPoisonEffect);
		player.EffectsManager.ApplyEffect(stackingPoisonEffect);

		// Simulate 6 seconds of game time
		player.EffectsManager.UpdateEffects(6f);

		Console.WriteLine("Poison drainEffect applied with stacking up to 3 times.");

		// Assuming initial health was 100, 3 damage per stack, 3 stacks applied, 3 ticks of damage (2 seconds each)
		// = 9 total damage
		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(100 - 27);
	}


	[Fact]
	[Trait("Quick Start", null)]
	public void Unique_effect_example()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Define the unique effect data
		var uniqueEffectData = new EffectData(
			"Unique Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10.0f))), // Lasts 10 seconds
			new[] {
				new Modifier(
					"PlayerAttributeSet.Strength",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(5.0f, new Curve(new[]
						{
							new CurveKey(1, 1.0f), // Level 1: base value × 1 = +5 Strength
							new CurveKey(2, 2.0f)  // Level 2: base value × 2 = +10 Strength
						}))
					)
				)
			},
			stackingData: new StackingData(
				StackLimit: new ScalableInt(1), // Only 1 instance allowed
				InitialStack: new ScalableInt(1), // Starts with 1 stack
				OverflowPolicy: StackOverflowPolicy.AllowApplication, // Allow application even if max stacks reached
				MagnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
				ExpirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				StackPolicy: StackPolicy.AggregateByTarget, // Only one effect per target
				OwnerDenialPolicy: StackOwnerDenialPolicy.AlwaysAllow, // Always allow application regardless of owner
				OwnerOverridePolicy: StackOwnerOverridePolicy.Override, // Override existing effect if applied again
				OwnerOverrideStackCountPolicy: StackOwnerOverrideStackCountPolicy.ResetStacks,
				StackLevelPolicy: StackLevelPolicy.AggregateLevels, // Aggregate levels of the effect
				LevelOverridePolicy: LevelComparison.Equal | LevelComparison.Higher,
				LevelDenialPolicy: LevelComparison.Lower, // Deny lower-level effects
				LevelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.ResetStacks
			)
		);

		// Apply the unique effect at level 1
		var uniqueEffectLevel1 = new Effect(uniqueEffectData, new EffectOwnership(player, player), 1);
		player.EffectsManager.ApplyEffect(uniqueEffectLevel1);

		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(15); // Assuming base strength was 10

		// Apply the unique effect at level 2 (overrides level 1)
		var uniqueEffectLevel2 = new Effect(uniqueEffectData, new EffectOwnership(player, player), 2);
		player.EffectsManager.ApplyEffect(uniqueEffectLevel2);

		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(20); // Assuming base strength was 10
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Adding_a_temporary_tag_with_an_effect()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a "Stunned" effect that adds a tag and reduces speed to 0
		var stunEffectData = new EffectData(
			"Stunned",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(3.0f))), // 3 seconds duration
			new[] {
				new Modifier(
					"PlayerAttributeSet.Speed",
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(0) // Set speed to 0
					)
				)
			},
			effectComponents: new[] {
				new ModifierTagsEffectComponent(
					tagsManager.RequestTagContainer(new[] { "status.stunned" })
				)
			}
		);

		// Apply the stun effect
		var stunEffect = new Effect(stunEffectData, new EffectOwnership(player, player));
		ActiveEffectHandle? stunHandle = player.EffectsManager.ApplyEffect(stunEffect);

		// Check if player is stunned
		bool isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
		int currentSpeed = player.Attributes["PlayerAttributeSet.Speed"].CurrentValue;

		stunHandle.Should().NotBeNull();
		isStunned.Should().BeTrue();
		currentSpeed.Should().Be(0);
	}


	[Fact]
	[Trait("Quick Start", null)]
	public void Preventing_effect_application_based_on_tags()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create an effect that cannot be applied if the target has the "status.immune.fire" tag
		var fireAttackData = new EffectData(
			"Fire Attack",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-15) // -15 damage
					)
				)
			},
			effectComponents: new[] {
				new TargetTagRequirementsEffectComponent(
					applicationTagRequirements: new TagRequirements(
						// Prevent application if target has "status.immune.fire"
						IgnoreTags: tagsManager.RequestTagContainer(new[] { "status.immune.fire" })
					)
				)
			}
		);

		// Apply the fire attack effect
		var fireAttack = new Effect(fireAttackData, new EffectOwnership(player, player));
		bool applied = player.EffectsManager.ApplyEffect(fireAttack) is not null;

		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(85); // Assuming initial health was 100
		applied.Should().BeFalse();
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Advanced_custom_components()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Create a "Stunned" effect that adds a tag and reduces speed to 0
		var stunEffectData = new EffectData(
			"Stunned",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(3.0f))), // 3 seconds duration
			new[] {
				new Modifier(
					"PlayerAttributeSet.Speed",
					ModifierOperation.Override,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(0) // Set speed to 0
					)
				)
			},
			effectComponents: new[] {
				new ModifierTagsEffectComponent(
					tagsManager.RequestTagContainer(new[] { "status.stunned" })
				)
			}
		);

		var stunEffect = new Effect(stunEffectData, new EffectOwnership(player, player));

		// Create a damage effect with threshold component
		var thresholdAttackData = new EffectData(
			"Basic Attack",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-10) // -10 damage
					)
				)
			},
			effectComponents: [
				new DamageThresholdComponent(90, stunEffect) // Apply stun if health drops below 90
			]
		);

		// Apply the effect
		var thresholdAttack = new Effect(thresholdAttackData, new EffectOwnership(player, player));
		player.EffectsManager.ApplyEffect(thresholdAttack);

		// Check if the stun was applied (will be true if health was 90 or less after damage)
		bool isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
		isStunned.Should().BeFalse();

		player.EffectsManager.ApplyEffect(thresholdAttack);
		isStunned = player.Tags.CombinedTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned"));
		isStunned.Should().BeTrue();
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Advanced_creating_a_custom_calculator()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Use the custom calculator in an effect
		var calculatedDamageEffectData = new EffectData(
			"Power Attack",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.CustomCalculatorClass,
						customCalculationBasedFloat: new CustomCalculationBasedFloat(
							new StrengthDamageCalculator(),
							new ScalableFloat(1.0f),  // Coefficient
							new ScalableFloat(0),     // PreMultiply
							new ScalableFloat(0)      // PostMultiply
						)
					)
				)
			}
		);

		// Apply the effect
		var calculatedDamageEffect = new Effect(calculatedDamageEffectData, new EffectOwnership(player, player));
		player.EffectsManager.ApplyEffect(calculatedDamageEffect);

		// Assuming initial health was 100 and strength was 10, so damage is 10 + (10 * 0.5) = 15
		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(85);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Advanced_creating_a_custom_execution()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player1 = new Player(tagsManager, cuesManager);
		var player2 = new Player(tagsManager, cuesManager);

		// Use the custom execution in an effect
		var drainEffectData = new EffectData(
			"Life Drain",
			new DurationData(DurationType.Instant),
			customExecutions: new[] {
		new HealthDrainExecution()
			}
		);

		// Apply the effect (using player1 as source and player2 as target)
		var drainEffect = new Effect(drainEffectData, new EffectOwnership(player1, player1));
		player2.EffectsManager.ApplyEffect(drainEffect);

		// Gains 4 health (80% of 5 drained)
		player1.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(104);
		// Drains 5 health
		player2.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(95);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Triggering_a_cue_through_effects()
	{
		// Arrange
		var mockCueHandler = tagsAndCueFixture.MockCueHandler;
		mockCueHandler.Reset();

		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Define a burning effect that includes the fire damage cue
		var burningEffectData = new EffectData(
			"Burning",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(5.0f))),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Health",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-5) // Damage per tick
					)
				)
			},
			periodicData: new PeriodicData(
				Period: new ScalableFloat(1.0f), // Ticks every second
				ExecuteOnApplication: true,
				PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			),
			cues: new[] {
				new CueData(
					CueTags: tagsManager.RequestTagContainer(new[] { "cues.damage.fire" }),
					MinValue: 0,
					MaxValue: 100,
					MagnitudeType: CueMagnitudeType.AttributeValueChange,
					MagnitudeAttribute: "PlayerAttributeSet.Health" // Tracks health changes
				)
			}
		);

		var burningEffect = new Effect(burningEffectData, new EffectOwnership(player, player));

		// Act
		player.EffectsManager.ApplyEffect(burningEffect);
		player.EffectsManager.UpdateEffects(5f); // Simulate 5 seconds of game time

		// Assert
		mockCueHandler.ApplyCount.Should().Be(1);
		mockCueHandler.ExecuteCount.Should().Be(6); // 1 on application + 5 from periodic ticks
		mockCueHandler.Magnitudes.Should().OnlyContain(x => x == -5);
		mockCueHandler.RemoveCount.Should().Be(1);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Manually_triggering_a_cue()
	{
		// Arrange
		var mockCueHandler = tagsAndCueFixture.MockCueHandler;
		mockCueHandler.Reset();

		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Manually trigger a fire damage cue
		var parameters = new CueParameters(
			Magnitude: 25, // Raw damage value
			NormalizedMagnitude: 0.25f, // Normalized between 0-1
			Source: player,
			CustomParameters: new Dictionary<StringKey, object>
			{
				{ "DamageType", "Fire" },
				{ "IsCritical", true }
			}
		);

		// Act
		cuesManager.ExecuteCue(
			cueTag: Tag.RequestTag(tagsManager, "cues.damage.fire"),
			target: player,
			parameters: parameters
		);

		// Assert
		mockCueHandler.ExecuteCount.Should().Be(1);
		mockCueHandler.Magnitudes.Should().Contain(25);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Subscribing_and_raising_an_event()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);
		var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");

		float receivedDamage = 0f;
		bool eventFired = false;

		player.Events.Subscribe(damageTag, eventData =>
		{
			eventFired = true;
			receivedDamage = eventData.EventMagnitude;
		});

		player.Events.Raise(new EventData
		{
			EventTags = damageTag.GetSingleTagContainer(),
			Source = null,
			Target = player,
			EventMagnitude = 50f
		});

		eventFired.Should().BeTrue();
		receivedDamage.Should().Be(50f);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Strongly_typed_events()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);
		var damageTag = Tag.RequestTag(tagsManager, "events.combat.damage");

		string logMessage = string.Empty;
		int logValue = 0;

		// Subscribe with generic type
		player.Events.Subscribe<CombatLogPayload>(damageTag, eventData =>
		{
			logMessage = eventData.Payload.Message;
			logValue = eventData.Payload.Value;
		});

		// Raise with generic type
		player.Events.Raise(new EventData<CombatLogPayload>
		{
			EventTags = damageTag.GetSingleTagContainer(),
			Source = null,
			Target = player,
			Payload = new CombatLogPayload("Critical Hit", 9999)
		});

		logMessage.Should().Be("Critical Hit");
		logValue.Should().Be(9999);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Granting_activating_and_removing_an_ability()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);

		var fireballCostEffect = new EffectData(
			"Fireball Mana Cost",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Mana",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-20) // -20 mana cost
					)
				)
			});

		var fireballCooldownEffect = new EffectData(
			"Fireball Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10.0f))), // 10 seconds cooldown
			effectComponents: new[] {
				new ModifierTagsEffectComponent(
					tagsManager.RequestTagContainer(new[] { "cooldown.fireball" })
				)
			});

		var fireballData = new AbilityData(
			name: "Fireball",
			costEffect: fireballCostEffect,
			cooldownEffects: [fireballCooldownEffect],
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			behaviorFactory: () => new CustomAbilityBehavior("Fireball"));

		var grantConfig = new GrantAbilityConfig
		{
			AbilityData = fireballData,
			ScalableLevel = new ScalableInt(1),
			LevelOverridePolicy = LevelComparison.None,
			RemovalPolicy = AbilityDeactivationPolicy.CancelImmediately,
			InhibitionPolicy = AbilityDeactivationPolicy.CancelImmediately,
		};

		var grantFireballEffect = new EffectData(
			"Grant Fireball Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]
			);

		var grantEffectHandle = player.EffectsManager.ApplyEffect(
			new Effect(grantFireballEffect, new EffectOwnership(player, player)));

		// Retrieve handle directly from component as shown in docs
		var fireballAbilityHandle = grantEffectHandle.GetComponent<GrantAbilityEffectComponent>().GrantedAbilities[0];

		bool successfulActivation = fireballAbilityHandle.Activate(out AbilityActivationFailures failures);

		successfulActivation.Should().BeTrue();
		failures.Should().Be(AbilityActivationFailures.None);
		fireballAbilityHandle.IsActive.Should().BeFalse();

		player.EffectsManager.RemoveEffect(grantEffectHandle);
		fireballAbilityHandle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Activating_an_ability_with_checks()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);

		// Setup ability with cost and cooldown
		var fireballCostEffect = new EffectData(
			"Fireball Mana Cost",
			new DurationData(DurationType.Instant),
			new[] {
				new Modifier(
					"PlayerAttributeSet.Mana",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(-20)
					)
				)
			});

		var fireballCooldownEffect = new EffectData(
			"Fireball Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10.0f))),
			effectComponents: new[] {
				new ModifierTagsEffectComponent(
					tagsManager.RequestTagContainer(new[] { "cooldown.fireball" })
				)
			});

		var fireballData = new AbilityData(
			name: "Fireball",
			costEffect: fireballCostEffect,
			cooldownEffects: [fireballCooldownEffect],
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			behaviorFactory: () => new CustomAbilityBehavior("Fireball"));

		// Grant permanently
		AbilityHandle handle = player.Abilities.GrantAbilityPermanently(
			fireballData,
			abilityLevel: 1,
			levelOverridePolicy: LevelComparison.None,
			sourceEntity: player);

		// Check Cooldown
		var cooldowns = handle.GetCooldownData();
		cooldowns.Should().NotBeEmpty();
		cooldowns[0].RemainingTime.Should().Be(0);

		// Check Cost
		var costs = handle.GetCostData();
		costs.Should().Contain(c => c.Attribute == "PlayerAttributeSet.Mana" && c.Cost == -20);

		// Activate
		bool success = handle.Activate(out AbilityActivationFailures failures);
		success.Should().BeTrue();

		// Verify resources consumed and cooldown started
		player.Attributes["PlayerAttributeSet.Mana"].CurrentValue.Should().Be(80);
		handle.GetCooldownData()[0].RemainingTime.Should().BeGreaterThan(0);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Granting_an_ability_and_activating_once()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);

		// Simple fireball data
		var fireballData = new AbilityData(
			name: "Fireball",
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			behaviorFactory: () => new CustomAbilityBehavior("Fireball"));

		// Simulate using a scroll
		AbilityHandle? handle = player.Abilities.GrantAbilityAndActivateOnce(
			abilityData: fireballData,
			abilityLevel: 1,
			levelOverridePolicy: LevelComparison.None,
			out AbilityActivationFailures failureFlags,
			targetEntity: player, // Target of the fireball
			sourceEntity: player  // Source (e.g., the scroll item)
		);

		// Fireball ends instantly so handle is null
		handle.Should().BeNull();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Triggering_an_ability_through_an_event()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);
		var hitTag = Tag.RequestTag(tagsManager, "events.combat.hit");

		var autoShieldData = new AbilityData(
			name: "Auto Shield",
			// Configure the trigger
			abilityTriggerData: AbilityTriggerData.ForEvent(hitTag),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			behaviorFactory: () => new CustomAbilityBehavior("Auto Shield"));

		var handle = player.Abilities.GrantAbilityPermanently(autoShieldData, 1, LevelComparison.None, player);

		handle!.IsActive.Should().BeFalse();

		player.Events.Raise(new EventData
		{
			EventTags = hitTag.GetSingleTagContainer(),
		});

		// Ability ends itself instantly so it should be false here
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Triggering_an_ability_through_tags()
	{
		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		var player = new Player(tagsManager, cuesManager);
		var rageTag = Tag.RequestTag(tagsManager, "status.enraged");

		// Ability configuration
		var rageAbilityData = new AbilityData(
			"Rage Aura",
			abilityTriggerData: AbilityTriggerData.ForTagPresent(rageTag),
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			// Using a persistent behavior to verify active state
			behaviorFactory: () => new PersistentAbilityBehavior());

		// Grant permanently
		var handle = player.Abilities.GrantAbilityPermanently(rageAbilityData, 1, LevelComparison.None, player);

		handle.IsActive.Should().BeFalse();

		// Apply effect that adds the tag
		var enrageEffect = new EffectData(
			"Enrage",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					new ScalableFloat(10f))),
			effectComponents: [
				new ModifierTagsEffectComponent(tagsManager.RequestTagContainer(["status.enraged"]))
			]);

		var effectHandle = player.EffectsManager.ApplyEffect(
			new Effect(enrageEffect, new EffectOwnership(player, player)));

		// Should activate automatically
		handle.IsActive.Should().BeTrue();

		// Remove effect (removes tag)
		player.EffectsManager.RemoveEffect(effectHandle);

		// Should deactivate automatically
		handle.IsActive.Should().BeFalse();
	}

	public class PlayerAttributeSet : AttributeSet
	{
		public EntityAttribute Health { get; }
		public EntityAttribute Mana { get; }
		public EntityAttribute Strength { get; }
		public EntityAttribute Speed { get; }

		public PlayerAttributeSet()
		{
			Health = InitializeAttribute(nameof(Health), 100, 0, 150);
			Mana = InitializeAttribute(nameof(Mana), 100, 0, 100);
			Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
			Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
		}
	}

	public class Player : IForgeEntity
	{
		public EntityAttributes Attributes { get; }
		public EntityTags Tags { get; }
		public EffectsManager EffectsManager { get; }
		public EntityAbilities Abilities { get; }

		public EventManager Events { get; }

		public Player(TagsManager tagsManager, CuesManager cuesManager)
		{
			// Initialize base tags during construction
			var baseTags = new TagContainer(
				tagsManager,
				[
					Tag.RequestTag(tagsManager, "character.player"),
					Tag.RequestTag(tagsManager, "class.warrior")
				]);

			Attributes = new EntityAttributes(new PlayerAttributeSet());
			Tags = new EntityTags(baseTags);
			EffectsManager = new EffectsManager(this, cuesManager);
			Abilities = new(this);
			Events = new();
		}
	}

	// Custom component that applies extra effect when Health is bellow threshold at the time of effect application
	public class DamageThresholdComponent : IEffectComponent
	{
		private readonly float _threshold;
		private readonly Effect _bonusEffect;

		public DamageThresholdComponent(float threshold, Effect bonusEffect)
		{
			_threshold = threshold;
			_bonusEffect = bonusEffect;
		}

		public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
		{
			// Track damage and apply the bonus effect if threshold is met
			var health = target.Attributes["PlayerAttributeSet.Health"];
			if (health.CurrentValue <= _threshold)
			{
				target.EffectsManager.ApplyEffect(_bonusEffect);
			}
		}
	}

	public class StrengthDamageCalculator : CustomModifierMagnitudeCalculator
	{
		public AttributeCaptureDefinition StrengthAttribute { get; }
		public AttributeCaptureDefinition SpeedAttribute { get; }

		public StrengthDamageCalculator()
		{
			StrengthAttribute = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Strength",
				AttributeCaptureSource.Source,
				Snapshot: true);

			SpeedAttribute = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Speed",
				AttributeCaptureSource.Source,
				Snapshot: true);

			AttributesToCapture.Add(StrengthAttribute);
			AttributesToCapture.Add(SpeedAttribute);
		}

		public override float CalculateBaseMagnitude(
				Effect effect,
				IForgeEntity target,
				EffectEvaluatedData? effectEvaluatedData)
		{
			int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target, effectEvaluatedData);
			int speed = CaptureAttributeMagnitude(SpeedAttribute, effect, target, effectEvaluatedData);

			// Base damage plus 50% of strength
			float damage = (speed * 2) + (strength * 0.5f);

			return -damage;  // Negative for damage
		}
	}

	public class HealthDrainExecution : CustomExecution
	{
		// Define attributes to capture and modify
		public AttributeCaptureDefinition TargetHealth { get; }
		public AttributeCaptureDefinition SourceHealth { get; }
		public AttributeCaptureDefinition SourceStrength { get; }

		public HealthDrainExecution()
		{
			// Capture target mana and magic resistance
			TargetHealth = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Health",
				AttributeCaptureSource.Target,
				Snapshot: false);

			SourceHealth = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Health",
				AttributeCaptureSource.Source,
				Snapshot: false);

			SourceStrength = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Strength",
				AttributeCaptureSource.Source,
				Snapshot: false);

			// Register attributes for capture
			AttributesToCapture.Add(TargetHealth);
			AttributesToCapture.Add(SourceHealth);
			AttributesToCapture.Add(SourceStrength);
		}

		public override ModifierEvaluatedData[] EvaluateExecution(
			Effect effect,
			IForgeEntity target,
			EffectEvaluatedData effectEvaluatedData)
		{
			var results = new List<ModifierEvaluatedData>();

			// Get attribute values
			int targetHealth = CaptureAttributeMagnitude(
				TargetHealth,
				effect,
				target,
				effectEvaluatedData);
			int sourceHealth = CaptureAttributeMagnitude(
				SourceHealth,
				effect,
				effect.Ownership.Owner,
				effectEvaluatedData);
			int sourceStrength = CaptureAttributeMagnitude(
				SourceStrength,
				effect,
				effect.Ownership.Owner,
				effectEvaluatedData);

			// Calculate health drain amount based on source strength
			float drainAmount = sourceStrength * 0.5f;

			// Cap the drain at the target's available health
			drainAmount = Math.Min(drainAmount, targetHealth);

			// Apply health reduction to target if attribute exists
			if (TargetHealth.TryGetAttribute(target, out EntityAttribute? targetHealthAttribute))
			{
				results.Add(new ModifierEvaluatedData(
					targetHealthAttribute,
					ModifierOperation.FlatBonus,
					-drainAmount,  // Negative for drain
					channel: 0
				));
			}

			// Apply health gain to source if attribute exists
			if (SourceHealth.TryGetAttribute(effect.Ownership.Source, out EntityAttribute? sourceManaAttribute))
			{
				results.Add(new ModifierEvaluatedData(
					sourceManaAttribute,
					ModifierOperation.FlatBonus,
					drainAmount * 0.8f,  // Transfer 80% of drained health
					channel: 0
				));
			}

			return results.ToArray();
		}
	}

	public class MockCueHandler : ICueHandler
	{
		public int ApplyCount { get; private set; }
		public int ExecuteCount { get; private set; }
		public int RemoveCount { get; private set; }
		public List<float> Magnitudes { get; } = new();

		public void OnApply(IForgeEntity? target, CueParameters? parameters) => ApplyCount++;

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			ExecuteCount++;
			if (parameters.HasValue)
			{
				Magnitudes.Add(parameters.Value.Magnitude);
			}
		}

		public void OnRemove(IForgeEntity? target, bool interrupted) => RemoveCount++;

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters) { }

		public void Reset()
		{
			ApplyCount = 0;
			ExecuteCount = 0;
			RemoveCount = 0;
			Magnitudes.Clear();
		}
	}

	private class CustomAbilityBehavior(string parameter) : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			context.AbilityHandle.CommitAbility();

			// Instantiate a projectile here (omitted for brevity)
			Console.WriteLine($"{context.Owner} used ability ({parameter}) on target {context.Target}");

			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// Cleanup if necessary
		}
	}

	private class PersistentAbilityBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			context.AbilityHandle.CommitAbility();
			// Does NOT call End() to simulate a persistent effect/aura
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// Cleanup
		}
	}

	public record struct CombatLogPayload(string Message, int Value);
}
