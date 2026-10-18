# Extending Effects

When the built-in configuration is not enough: custom components, composed components, custom magnitude calculators and custom executions. Every guide here builds on the same small setup: a `PlayerAttributeSet`, a `Player` entity, and a `TagsManager` and `CuesManager`. It is defined once in [Guides](README.md#the-shared-setup); the code samples assume it.

## Custom Components

You can extend effects with custom logic using components.

This component applies an additional effect when the target's Health is already at or below a threshold as the effect is applied, before the effect's own damage lands. `stunEffect` is the stun from [Effect Recipes](effect-recipes.md#adding-a-temporary-tag-with-an-effect):

```csharp
// Custom component that applies extra effect when Health is below threshold
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
        // Check if target health is below threshold and apply bonus effect if it is
        var health = target.Attributes["PlayerAttributeSet.Health"];
        if (health.CurrentValue <= _threshold)
        {
            target.EffectsManager.ApplyEffect(_bonusEffect);
        }
    }
}

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
        new DamageThresholdComponent(90, stunEffect) // Stun if Health is already at or below 90
    ]
);

// The first hit finds Health at 100 and leaves it at 90; the second finds 90 and stuns
var thresholdAttack = new Effect(thresholdAttackData, new EffectOwnership(player, player));
player.EffectsManager.ApplyEffect(thresholdAttack);
player.EffectsManager.ApplyEffect(thresholdAttack);

bool isStunned = player.Tags.AllTags.HasTag(Tag.RequestTag(tagsManager, "status.stunned")); // true
```

## Composing Components

The section above wrote a custom component. Often you don't have to: several built-in components on one effect can express mechanics that look like they need custom code.

A curse that damages its victim over time and, when it wears off, heals its caster for everything it dealt. Three pieces line up:

```csharp
// Two entities this time: the one the curse lands on, and the one it pays back
var caster = new Player(tagsManager, cuesManager);
var victim = new Player(tagsManager, cuesManager);

// The tag the running total is published under, and the tag the payout reads it back from
var damageDealtTag = Tag.RequestTag(tagsManager, "data.damage.dealt");

// 1. The heal reads its magnitude from the tag rather than from a fixed number
var siphonData = new EffectData(
    "Curse Siphon",
    new DurationData(DurationType.Instant),
    [
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(
                MagnitudeCalculationType.SetByCaller,
                setByCallerFloat: new SetByCallerFloat(damageDealtTag)))
    ]);

// 2. The curse ticks damage, tallies what actually landed, and pays out to the source on the way out
var curseData = new EffectData(
    "Curse",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5.0f))),
    [
        new Modifier(
            "PlayerAttributeSet.Health",
            ModifierOperation.FlatBonus,
            new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
    ],
    periodicData: new PeriodicData(
        Period: new ScalableFloat(1.0f),
        ExecuteOnApplication: true,
        PeriodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.NeverReset),
    effectComponents: [
        // Tallies how much this application actually took off the victim's health
        new AttributeAccumulatorEffectComponent("PlayerAttributeSet.Health", damageDealtTag),
        new AdditionalEffectsEffectComponent(
            onCompleteAlways: [
                new ConditionalEffect(siphonData, Target: EffectApplicationTarget.Source)
            ],
            copyDataFromOriginalEffect: true) // carries the tally to the heal
    ]);

// 3. Build a fresh Effect per cast, so the tally belongs to this curse and no other
victim.EffectsManager.ApplyEffect(new Effect(curseData, new EffectOwnership(caster, caster)));

// Simulate 5 seconds of game time
victim.EffectsManager.UpdateEffects(5f);

// 6 ticks of -5 (including the initial execute), so 30 dealt and 30 paid back, both starting at 100
int victimHealth = victim.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 70
int casterHealth = caster.Attributes["PlayerAttributeSet.Health"].CurrentValue; // 130
```

No `CustomExecution`, no custom component, and no meta-attribute concept. Each of the three pieces does one job:

- [`AttributeAccumulatorEffectComponent`](../effects/components/attribute-accumulator-effect-component.md) measures how much the attribute actually moved and publishes the running total as a `SetByCaller` magnitude. It measures the attribute rather than the effect's declared modifiers, so the payout is worth what the curse *managed* to deal rather than what it aimed at — the difference shows up the moment a victim dies with damage to spare.
- [`AdditionalEffectsEffectComponent`](../effects/components/additional-effects-effect-component.md) applies the heal when the curse ends, aimed at `EffectApplicationTarget.Source` rather than the victim.
- `copyDataFromOriginalEffect` is what carries the tally from the curse to the heal. Without it the payout is built with an empty magnitude dictionary and cannot resolve the tag at all.

Two things to know before building on this:

- **The tally lives on the `Effect`, which is shared if you reuse one.** `DataTag` belongs to the `Effect` instance rather than to the application, so one `Effect` applied to three enemies has one dictionary and the three tallies collide. Build a fresh `Effect` per cast — which is what a targeted curse does anyway.
- **The payout reads the total once, as it is applied.** That is right here, since the heal is built at the moment the curse ends and sees the final figure. A magnitude that has to follow a total *while* it climbs — an event reporting damage per tick, say — needs its `SetByCallerFloat` declared non-snapshot, or it caches the first value it ever read.

## Creating a Custom Calculator

Custom Calculators let you create modifiers that depend on multiple attributes.

This calculator calculates damage based on both strength and speed attributes: `(speed * 2) + (strength * 0.5f)`:

```csharp
// Create a custom calculator that scales damage based on strength and speed
public class StrengthDamageCalculator : CustomModifierMagnitudeCalculator
{
    public AttributeCaptureDefinition StrengthAttribute { get; }
    public AttributeCaptureDefinition SpeedAttribute { get; }

    public StrengthDamageCalculator()
    {
        StrengthAttribute = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Strength",
            AttributeCaptureSource.Owner,
            Snapshot: true);

        SpeedAttribute = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Speed",
            AttributeCaptureSource.Owner,
            Snapshot: true);

        AttributesToCapture.Add(StrengthAttribute);
        AttributesToCapture.Add(SpeedAttribute);
    }

    public override float CalculateBaseMagnitude(Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
    {
        int strength = CaptureAttributeMagnitude(StrengthAttribute, effect, target, effectEvaluatedData);
        int speed = CaptureAttributeMagnitude(SpeedAttribute, effect, target, effectEvaluatedData);

        // Calculate damage based on strength and speed
        float damage = (speed * 2) + (strength * 0.5f);

        return -damage;  // Negative for damage
    }
}

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
```

## Creating a Custom Execution

Custom Executions allow you to modify multiple attributes with a single calculation.

This execution drains health from the target based on the source's strength, and returns 80% of that health to the source:

```csharp
public class HealthDrainExecution : CustomExecution
{
    // Define attributes to capture and modify
    public AttributeCaptureDefinition TargetHealth { get; }
    public AttributeCaptureDefinition SourceHealth { get; }
    public AttributeCaptureDefinition SourceStrength { get; }

    public HealthDrainExecution()
    {
        // Capture target health
        TargetHealth = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Health",
            AttributeCaptureSource.Target,
            Snapshot: false);

        SourceHealth = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Health",
            AttributeCaptureSource.Owner,
            Snapshot: false);

        SourceStrength = new AttributeCaptureDefinition(
            "PlayerAttributeSet.Strength",
            AttributeCaptureSource.Owner,
            Snapshot: false);

        // Register attributes for capture
        AttributesToCapture.Add(TargetHealth);
        AttributesToCapture.Add(SourceHealth);
        AttributesToCapture.Add(SourceStrength);
    }

    public override ModifierEvaluatedData[] EvaluateExecution(Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
    {
        var results = new List<ModifierEvaluatedData>();

        // Get attribute values
        int targetHealth = CaptureAttributeMagnitude(TargetHealth, effect, target, effectEvaluatedData);
        int sourceStrength = CaptureAttributeMagnitude(SourceStrength, effect, effect.Ownership.Source, effectEvaluatedData);

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
        if (SourceHealth.TryGetAttribute(effect.Ownership.Source, out EntityAttribute? sourceHealthAttribute))
        {
            results.Add(new ModifierEvaluatedData(
                sourceHealthAttribute,
                ModifierOperation.FlatBonus,
                drainAmount * 0.8f,  // Transfer 80% of drained health
                channel: 0
            ));
        }

        return results.ToArray();
    }
}

// Use the custom execution in an effect
var drainEffectData = new EffectData(
    "Life Drain",
    new DurationData(DurationType.Instant),
    customExecutions: new[] {
        new HealthDrainExecution()
    }
);

// player1 drains player2
var player1 = new Player(tagsManager, cuesManager);
var player2 = new Player(tagsManager, cuesManager);

var drainEffect = new Effect(drainEffectData, new EffectOwnership(player1, player1));
player2.EffectsManager.ApplyEffect(drainEffect);

// Strength 10 drains 5: player2 drops to 95 and player1 gains 80% of it, to 104
```

