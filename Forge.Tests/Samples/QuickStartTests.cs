// Copyright © Gamesmiths Guild.

#pragma warning disable

using System.Buffers.Text;
using FluentAssertions;
using Gamesmiths;
using Gamesmiths.Forge;
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // 10 seconds duration
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

		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(20); // Assuming base strength was 10
		player.Attributes["PlayerAttributeSet.Strength"].ValidModifier.Should().Be(10); // +10 from buff

		// Remove the effect manually (e.g., when the item is unequipped)
		if (activeEffectHandle is not null)
		{
			player.EffectsManager.UnapplyEffect(activeEffectHandle);
		}

		player.Attributes["PlayerAttributeSet.Strength"].CurrentValue.Should().Be(10); // Assuming base strength was 10
		player.Attributes["PlayerAttributeSet.Strength"].ValidModifier.Should().Be(0); // 0 because the effect was removed
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)),
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
				period: new ScalableFloat(2.0f),
				executeOnApplication: true,
				periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			)
		);

		// Apply the poison effect
		var poisonEffect = new Effect(poisonEffectData, new EffectOwnership(player, player));
		player.EffectsManager.ApplyEffect(poisonEffect);

		player.EffectsManager.UpdateEffects(10f); // Simulate 10 seconds of game time

		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(70); // Assuming initial health was 100, 5 damage every 2 seconds for 10 seconds (5 ticks) + 1 initial tick
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(6.0f)), // Each stack lasts 6 seconds
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
				period: new ScalableFloat(2.0f),
				executeOnApplication: false,
				periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			),
			stackingData: new StackingData(
				stackLimit: new ScalableInt(3), // Max 3 stacks
				initialStack: new ScalableInt(1), // Starts with 1 stack
				overflowPolicy: StackOverflowPolicy.DenyApplication, // Deny if max stacks reached
				magnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
				expirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
				applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication, // Refresh duration on new stack
				stackPolicy: StackPolicy.AggregateBySource, // Aggregate stacks from the same source
				stackLevelPolicy: StackLevelPolicy.SegregateLevels, // Each stack can have its own level

				// The next two values must be defined because this is a periodic effect with stacking
				executeOnSuccessfulApplication: false, // Do not execute on successful application
				applicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.ResetOnSuccessfulApplication // Reset period on successful application
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

		// Assuming initial health was 100, 3 damage per stack, 3 stacks applied, 3 ticks of damage (2 seconds each) = 9 total damage
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(10.0f)), // Lasts 10 seconds
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
				stackLimit: new ScalableInt(1), // Only 1 instance allowed
				initialStack: new ScalableInt(1), // Starts with 1 stack
				overflowPolicy: StackOverflowPolicy.AllowApplication, // Allow application even if max stacks reached
				magnitudePolicy: StackMagnitudePolicy.Sum, // Total damage increases with stacks
				expirationPolicy: StackExpirationPolicy.ClearEntireStack, // All stacks expire together
				applicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication, // Refresh duration on new stack
				stackPolicy: StackPolicy.AggregateByTarget, // Only one effect per target
				ownerDenialPolicy: StackOwnerDenialPolicy.AlwaysAllow, // Always allow application regardless of owner
				ownerOverridePolicy: StackOwnerOverridePolicy.Override, // Override existing effect if applied again
				ownerOverrideStackCountPolicy: StackOwnerOverrideStackCountPolicy.ResetStacks, // Reset stack count on override
				stackLevelPolicy: StackLevelPolicy.AggregateLevels, // Aggregate levels of the effect
				levelOverridePolicy: LevelComparison.Equal | LevelComparison.Higher, // Allow equal or higher-level effects to override
				levelDenialPolicy: LevelComparison.Lower, // Deny lower-level effects
				levelOverrideStackCountPolicy: StackLevelOverrideStackCountPolicy.ResetStacks // Reset stack count on override
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(3.0f)), // 3 seconds duration
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
						ignoreTags: tagsManager.RequestTagContainer(new[] { "status.immune.fire" }) // Prevent application if target has "status.immune.fire"
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
			new DurationData(DurationType.HasDuration, new ScalableFloat(3.0f)), // 3 seconds duration
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

		player.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(85); // Assuming initial health was 100 and strength was 10, so damage is 10 + (10 * 0.5) = 15
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

		player1.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(104); // Gains 4 health (80% of 5 drained)
		player2.Attributes["PlayerAttributeSet.Health"].CurrentValue.Should().Be(95); // Drains 5 health
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Triggering_a_cue_through_effects()
	{
		// Arrange
		var stringWriter = new StringWriter();
		var originalOut = Console.Out;
		Console.SetOut(stringWriter);

		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Define a burning effect that includes the fire damage cue
		var burningEffectData = new EffectData(
			"Burning",
			new DurationData(DurationType.HasDuration, new ScalableFloat(5.0f)),
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
				period: new ScalableFloat(1.0f), // Ticks every second
				executeOnApplication: true,
				periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
			),
			cues: new[] {
				new CueData(
					cueTags: tagsManager.RequestTagContainer(new[] { "cues.damage.fire" }),
					minValue: 0,
					maxValue: 100,
					magnitudeType: CueMagnitudeType.AttributeValueChange,
					magnitudeAttribute: "PlayerAttributeSet.Health" // Tracks health changes
				)
			}
		);

		var burningEffect = new Effect(burningEffectData, new EffectOwnership(player, player));

		try
		{
			// Apply the burning effect
			player.EffectsManager.ApplyEffect(burningEffect);
			player.EffectsManager.UpdateEffects(5f); // Simulate 5 seconds of game time

			stringWriter.ToString().Should().Contain(
				"Fire damage cue applied to target.\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage executed: -5\r\n" +
				"Fire damage cue removed.");
		}
		finally
		{
			// Cleanup
			Console.SetOut(originalOut);
		}
	}

	[Fact]
	[Trait("Quick Start", null)]
	public void Manually_triggering_a_cue()
	{
		// Arrange
		var stringWriter = new StringWriter();
		var originalOut = Console.Out;
		Console.SetOut(stringWriter);

		// Initialize managers
		var tagsManager = _tagsManager;
		var cuesManager = _cuesManager;

		// Create player instance
		var player = new Player(tagsManager, cuesManager);

		// Manually trigger a fire damage cue
		var parameters = new CueParameters(
			magnitude: 25, // Raw damage value
			normalizedMagnitude: 0.25f, // Normalized between 0-1
			source: player,
			customParameters: new Dictionary<StringKey, object>
			{
				{ "DamageType", "Fire" },
				{ "IsCritical", true }
			}
		);

		try
		{
			cuesManager.ExecuteCue(
				cueTag: Tag.RequestTag(tagsManager, "cues.damage.fire"),
				target: player,
				parameters: parameters
			);

			stringWriter.ToString().Should().Contain("Fire damage executed: 25");
		}
		finally
		{
			// Cleanup
			Console.SetOut(originalOut);
		}

	}

	public class PlayerAttributeSet : AttributeSet
	{
		public EntityAttribute Health { get; }
		public EntityAttribute Strength { get; }
		public EntityAttribute Speed { get; }

		public PlayerAttributeSet()
		{
			Health = InitializeAttribute(nameof(Health), 100, 0, 150);
			Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
			Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
		}
	}

	public class Player : IForgeEntity
	{
		public EntityAttributes Attributes { get; }
		public EntityTags Tags { get; }
		public EffectsManager EffectsManager { get; }

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
				snapshot: true);

			SpeedAttribute = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Speed",
				AttributeCaptureSource.Source,
				snapshot: true);

			AttributesToCapture.Add(StrengthAttribute);
			AttributesToCapture.Add(SpeedAttribute);
		}

		public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target)
		{
			int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target);
			int speed = CaptureAttributeMagnitude(SpeedAttribute, effect, target);

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
				snapshot: false);

			SourceHealth = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Health",
				AttributeCaptureSource.Source,
				snapshot: false);

			SourceStrength = new AttributeCaptureDefinition(
				"PlayerAttributeSet.Strength",
				AttributeCaptureSource.Source,
				snapshot: false);

			// Register attributes for capture
			AttributesToCapture.Add(TargetHealth);
			AttributesToCapture.Add(SourceHealth);
			AttributesToCapture.Add(SourceStrength);
		}

		public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target)
		{
			var results = new List<ModifierEvaluatedData>();

			// Get attribute values
			int targetHealth = CaptureAttributeMagnitude(TargetHealth, effect, target);
			int sourceHealth = CaptureAttributeMagnitude(SourceHealth, effect, effect.Ownership.Owner);
			int sourceStrength = CaptureAttributeMagnitude(SourceStrength, effect, effect.Ownership.Owner);

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

	public class FireDamageCueHandler : ICueHandler
	{
		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			if (parameters.HasValue)
			{
				Console.WriteLine($"Fire damage executed: {parameters.Value.Magnitude}");
			}
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			// Logic for when a persistent cue starts (e.g., play fire animation)
			if (target != null)
			{
				Console.WriteLine("Fire damage cue applied to target.");
			}
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			// Logic for when a cue ends (e.g., stop fire animation)
			Console.WriteLine("Fire damage cue removed.");
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
			// Logic for updating persistent cues (e.g., adjust fire intensity)
			if (parameters.HasValue)
			{
				Console.WriteLine($"Fire damage cue updated with magnitude: {parameters.Value.Magnitude}");
			}
		}
	}
}
