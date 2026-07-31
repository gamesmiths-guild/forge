# ChanceToApplyEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.ChanceToApplyEffectComponent`
> **State:** Stateless — shared across every application
> **Applies to:** Any effect

Adds a random chance for an effect to be applied at all, with support for level-based scaling.

## Constructor

```csharp
new ChanceToApplyEffectComponent(randomProvider, chance)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| randomProvider | `IRandom` | The random source used to roll for application. Inject your game's generator so rolls stay deterministic and mockable. |
| chance | `ScalableFloat` | Probability in the range 0–1. Can scale with effect level through a curve. |

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `CanApplyEffect` | Rolls against `chance` and returns `false` to block the application entirely. |

## Behavior

The roll happens during validation, before any effect application logic runs. A failed roll means the effect never lands: no modifiers, no components, no cues.

### The IRandom Interface

The component draws from `IRandom` rather than `System.Random` directly, so the source can be swapped or mocked:

```csharp
public interface IRandom
{
    int NextInt();
    int NextInt(int maxValue);
    int NextInt(int minValue, int maxValue);
    int NextIntInclusive(int minValue, int maxValue);
    float NextSingle();
    float NextSingleInclusive();
    double NextDouble();
    double NextDoubleInclusive();
    long NextInt64();
    long NextInt64(long maxValue);
    long NextInt64(long minValue, long maxValue);
    long NextInt64Inclusive(long minValue, long maxValue);
    void NextBytes(byte[] buffer);
    void NextBytes(Span<byte> buffer);
}
```

The component specifically uses `NextSingle()`, which returns a value between 0.0 (inclusive) and 1.0 (exclusive). The interface also exposes explicit inclusive methods for APIs that need closed ranges without relying on helper conversions.

## Usage

```csharp
// Create a "Stun" effect with a 25% chance to apply
var stunEffectData = new EffectData(
    "Stun",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(3.0f)
        )
    ),
    effectComponents: new[] {
        new ChanceToApplyEffectComponent(
            randomProvider,  // Your game's random number generator
            new ScalableFloat(0.25f)  // 25% chance to apply
        )
    }
);
```

Scaling the chance with level:

```csharp
// Create a "Critical Hit" effect with a chance that scales with level
var criticalHitEffectData = new EffectData(
    "Critical Hit",
    new DurationData(DurationType.Instant),
    [/*...*/],
    effectComponents: new[] {
        new ChanceToApplyEffectComponent(
            randomProvider,
            new ScalableFloat(
                0.1f,  // Base 10% chance
                new Curve([
                    new CurveKey(1, 1.0f),   // Level 1: 10%
                    new CurveKey(5, 2.0f),   // Level 5: 20%
                    new CurveKey(10, 3.5f)   // Level 10: 35%
                ])
            )
        )
    }
);
```

## Key Points

- Uses the provided random provider for chance determination, so tests can inject a deterministic source.
- Chance can scale with effect level using `ScalableFloat`.
- Validates during `CanApplyEffect`, before any effect application logic.

## See Also

- [Effect Components Overview](README.md)
- [Modifiers](../modifiers.md)
