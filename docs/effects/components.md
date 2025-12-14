# Effect Components

Effect Components in Forge allows developers to extend effect functionality through a modular, composable approach. Components can add custom behaviors, validation logic, and react to different events in an effect's lifecycle.

For a practical guide on using components, see the [Quick Start Guide](../quick-start.md).

## Core Concept

Components follow the composition pattern, allowing you to build complex effect behaviors without inheritance. Each component implements the `IEffectComponent` interface and can be attached to any `EffectData`.

```csharp
public readonly struct EffectData(
    // Other parameters...
    IEffectComponent[]? effectComponents = null)
{
    // Implementation...
    public IEffectComponent[]? EffectComponents { get; }
}
```

## Implementing Custom Components

### The IEffectComponent Interface

To create a custom component, implement the `IEffectComponent` interface:

```csharp
public interface IEffectComponent
{
    bool CanApplyEffect(in IForgeEntity target, in Effect effect);
    bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData);
    void OnActiveEffectUnapplied(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData, bool removed);
    void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData);
    void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData);
    void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData);
}
```

The interface provides default implementations for all methods, so you only need to override the ones relevant to your component's functionality.

### Component Lifecycle Methods

#### CanApplyEffect

Called during the validation phase to determine if an effect can be applied. Return `false` to block the application.

```csharp
public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
{
    // Custom validation logic
    return true; // Allow application by default
}
```

Use cases:

- Checking if target meets requirements.
- Implementing application chances.
- Restricting effects based on game state.

#### OnActiveEffectAdded

Called when a non-instant effect is added to a target. Return `false` to inhibit the effect.

```csharp
public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
{
    // Custom initialization logic
    return true; // Keep the effect active by default
}
```

Use cases:

- Adding temporary tags or flags.
- Setting up event subscriptions.
- Initializing effect-specific game state.

#### OnActiveEffectUnapplied

Called when an effect is unapplied or a stack is removed.

```csharp
public void OnActiveEffectUnapplied(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData, bool removed)
{
    // Custom cleanup logic
    if (removed) {
        // Effect was completely removed
    } else {
        // Just a stack was removed
    }
}
```

Use cases:

- Removing temporary tags or flags.
- Cleaning up game state.
- Removing event subscriptions.

#### OnActiveEffectChanged

Called when an effect changes. This occurs specifically when:

- The effect level changes.
- Modifier values are updated.
- Stack count changes.
- Inhibition state changes.

```csharp
public void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
{
    // React to effect changes
}
```

Use cases:

- Updating related game systems.
- Adjusting dependent mechanics.

#### OnEffectApplied

Called for all effects when applied, including instant effects and stack applications.

```csharp
public void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
{
    // React to effect application
}
```

Use cases:

- Triggering reactions to both instant and duration effects.
- Cross-effect interactions.

#### OnEffectExecuted

Called when an instant or periodic effect executes its modifiers.

```csharp
public void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData)
{
    // React to effect execution
}
```

Use cases:

- Adding secondary effects based on execution results.
- Tracking execution statistics.
- Triggering additional gameplay reactions.

### Creating Custom Components

Example custom component:

```csharp
// Component that tracks damage thresholds and applies additional effects
public class DamageThresholdComponent : IEffectComponent
{
    private readonly Dictionary<ActiveEffectHandle, float> _accumulatedDamage = new();
    private readonly float _threshold;
    private readonly Effect _thresholdEffect;

    public DamageThresholdComponent(float threshold, Effect thresholdEffect)
    {
        _threshold = threshold;
        _thresholdEffect = thresholdEffect;
    }

    public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
    {
        _accumulatedDamage[activeEffectEvaluatedData.ActiveEffectHandle] = 0;
        // Note: This is a simplified example. A real implementation would need a more robust way to subscribe/unsubscribe.
        target.Attributes.GetAttribute("CombatAttributeSet.CurrentHealth").OnValueChanged +=
            (attribute, change) => TrackDamage(target, activeEffectEvaluatedData.ActiveEffectHandle, change);
        return true;
    }

    public void OnActiveEffectUnapplied(
        IForgeEntity target,
        in ActiveEffectEvaluatedData activeEffectEvaluatedData,
        bool removed)
    {
        if (removed)
        {
            _accumulatedDamage.Remove(activeEffectEvaluatedData.ActiveEffectHandle);
            // Note: This is a simplified example. A real implementation would need a more robust way to subscribe/unsubscribe.
            target.Attributes.GetAttribute("CombatAttributeSet.CurrentHealth").OnValueChanged -=
                (attribute, change) => TrackDamage(target, activeEffectEvaluatedData.ActiveEffectHandle, change);
        }
    }

    private void TrackDamage(IForgeEntity target, ActiveEffectHandle handle, int change)
    {
        if (change < 0 && _accumulatedDamage.ContainsKey(handle))
        {
            _accumulatedDamage[handle] += Math.Abs(change);

            if (_accumulatedDamage[handle] >= _threshold)
            {
                // Reset accumulation and apply threshold effect
                _accumulatedDamage[handle] = 0;
                target.EffectsManager.ApplyEffect(_thresholdEffect);
            }
        }
    }
}
```

### Advanced Component Integration

Components can be used to implement complex systems that integrate with your game's mechanics:

- **Combat Reaction System**: Components that trigger reactions between elements.
- **Cooldown Management**: Components that track and enforce cooldowns between effect applications.
- **Cross-Effect Coordination**: Components that coordinate between multiple active effects.
- **Attribute Threshold Monitoring**: Components that trigger effects when attributes cross thresholds.
- **AI Behavior Modification**: Components that adjust AI behavior when effects are active.

## Built-in Components

Forge includes several built-in components that demonstrate the component system's capabilities and provide ready-to-use functionality.

### ChanceToApplyEffectComponent

Adds a random chance for effects to be applied, with support for level-based scaling.

```csharp
public class ChanceToApplyEffectComponent(IRandom randomProvider, ScalableFloat chance) : IEffectComponent
{
    // Implementation...
}
```

#### The IRandom Interface

The `ChanceToApplyEffectComponent` uses the `IRandom` interface to generate random values for determining if an effect should be applied:

```csharp
public interface IRandom
{
    int NextInt();
    int NextInt(int maxValue);
    int NextInt(int minValue, int maxValue);
    float NextSingle();
    double NextDouble();
    long NextInt64();
    long NextInt64(long maxValue);
    long NextInt64(long minValue, long maxValue);
    void NextBytes(byte[] buffer);
    void NextBytes(Span<byte> buffer);
}
```

The component specifically uses the `NextSingle()` method, which returns a random floating-point number between 0.0 (inclusive) and 1.0 (exclusive). This allows for a consistent random number generation implementation that can be swapped or mocked for testing.

#### Usage Example

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

Advanced usage with level scaling:

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

Key points:

- Uses the provided random provider for chance determination.
- Chance can scale with effect level using `ScalableFloat`.
- Validates during `CanApplyEffect`, before any effect application logic.

### ModifierTagsEffectComponent

Adds tags to the target entity while the effect is active. These tags are automatically removed when the effect ends. See the [Tags documentation](../tags.md) for more on tags.

```csharp
public class ModifierTagsEffectComponent(TagContainer tagsToAdd) : IEffectComponent
{
    // Implementation...
}
```

Usage example:

```csharp
// Create a "Burning" effect that adds the "Status.Burning" tag to the target
var burningEffectData = new EffectData(
    "Burning",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(10.0f)
        )
    ),
    new[] {
        new Modifier("CombatAttributeSet.CurrentHealth", ModifierOperation.Add, new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-5)))
    },
    periodicData: new PeriodicData(
        period: new ScalableFloat(2.0f),
        executeOnApplication: true,
        periodInhibitionRemovedPolicy: PeriodInhibitionRemovedPolicy.ResetPeriod
    ),
    effectComponents: new[] {
        new ModifierTagsEffectComponent(
            tagsManager.RequestTagContainer(new[] { "status.burning" })
        )
    }
);
```

Key points:

- Only works with duration effects (not instant).
- Tags are automatically added when the effect is applied.
- Tags are automatically removed when the effect ends completely.
- With stacked effects, tags remain until all stacks are removed.

### TargetTagRequirementsEffectComponent

Validates if a target meets tag requirements for effect application and manages ongoing effect states based on tags.

```csharp
public class TargetTagRequirementsEffectComponent(
    TagRequirements applicationTagRequirements,
    TagRequirements removalTagRequirements,
    TagRequirements ongoingTagRequirements) : IEffectComponent
{
    // Implementation...
}
```

#### The TagRequirements System

The `TagRequirements` struct is a powerful mechanism for evaluating tag conditions on entities, used by the `TargetTagRequirementsEffectComponent`.

```csharp
public readonly struct TagRequirements(
    TagContainer? requiredTags = null,
    TagContainer? ignoreTags = null,
    TagQuery? tagQuery = null)
{
    // Implementation...
}
```

##### Components of TagRequirements

- **RequiredTags**: Tags that must all be present on the target.
- **IgnoreTags**: Tags that must not be present on the target (any match will fail).
- **TagQuery**: A complex query expression for advanced tag matching.

##### How TagRequirements Are Evaluated

```csharp
public bool RequirementsMet(in TagContainer targetContainer)
{
    var hasRequired = RequiredTags is null || targetContainer.HasAll(RequiredTags);
    var hasIgnored = IgnoreTags is not null && targetContainer.HasAny(IgnoreTags);
    var matchQuery = TagQuery is null || TagQuery.IsEmpty || TagQuery.Matches(targetContainer);

    return hasRequired && !hasIgnored && matchQuery;
}
```

For requirements to be met:

1. Target must have ALL required tags.
2. Target must have NONE of the ignore tags.
3. Target must match the tag query (if one is provided).

##### Tag Query Usage

Tag queries allow for more complex expressions than simple "has all" and "has none" logic. See the [Tags documentation](../tags.md) for more on tag queries.

```csharp
// Create a query that matches if:
// (Target has EITHER "Fire" OR "Ice") AND (Target does NOT have both "Water" AND "Metal")
var query = new TagQuery();
query.Build(new TagQueryExpression(tagsManager)
    .AllExpressionsMatch()
        .AddExpression(new TagQueryExpression(tagsManager)
            .AnyTagsMatch()
                .AddTag("Fire")
                .AddTag("Ice"))
        .AddExpression(new TagQueryExpression(tagsManager)
            .NoExpressionsMatch()
                .AddExpression(new TagQueryExpression(tagsManager)
                    .AllTagsMatch()
                        .AddTag("Water")
                        .AddTag("Metal"))));
```

#### Usage Example

```csharp
// Create a "Frost" effect that only applies to targets with the "Wet" tag,
// is removed if target gains the "Fire" tag, and is inhibited if target has the "Cold.Immune" tag
var frostEffectData = new EffectData(
    "Frost",
    new DurationData(
        DurationType.HasDuration,
        new ModifierMagnitude(
            MagnitudeCalculationType.ScalableFloat,
            new ScalableFloat(8.0f)
        )
    ),
    [/*...*/],
    effectComponents: new[] {
        new TargetTagRequirementsEffectComponent(
            // Application requirements: target must have "Wet" tag
            applicationTagRequirements: new TagRequirements(
                requiredTags: tagsManager.RequestTagContainer(new[] { "Wet" })
            ),
            // Removal requirements: effect is removed if target gets "Fire" tag
            removalTagRequirements: new TagRequirements(
                tagQuery: new TagQuery(tagsManager, "Fire")
            ),
            // Ongoing requirements: effect is inhibited if target has "Cold.Immune" tag
            ongoingTagRequirements: new TagRequirements(
                ignoreTags: tagsManager.RequestTagContainer(new[] { "Cold.Immune" })
            )
        )
    }
);
```

Key points:

- Dynamically monitors tag changes on the target.
- Can prevent application, force removal, or toggle inhibition.
- Automatically cleans up event subscriptions when the effect is removed.
- Uses `TagRequirements` to define complex tag conditions.

## Combining Components

Components can be combined to create complex effect behaviors:

```csharp
var complexEffectData = new EffectData(
    "Complex Effect",
    /* other parameters */
    effectComponents: new IEffectComponent[] {
        new ChanceToApplyEffectComponent(randomProvider, new ScalableFloat(0.5f)),
        new TargetTagRequirementsEffectComponent(/* requirements */),
        new ModifierTagsEffectComponent(/* tags to add */),
        new CustomEffectComponent() // Your own custom component
    }
);
```

## Best Practices

1. **Single Responsibility**: Each component should handle one specific aspect of behavior.
2. **Manage Resources**: Clean up any subscriptions or external resources in `OnActiveEffectUnapplied`.
3. **Consider Performance**: Components are called frequently, so optimize for performance.
4. **Use Return Values Correctly**: Return `false` from validation methods only when you want to block behavior.
5. **Leverage Existing Components**: Combine with built-in components when possible.
6. **Component Composition**: Use multiple simple components instead of one complex component.
7. **Avoid Circular Dependencies**: Be careful not to create recursive loops with components that apply effects.
8. **Error Handling**: Components should be robust against unexpected states and not throw exceptions.
9. **Documentation**: Document any requirements or assumptions your custom components make.
10. **Testing**: Test components in isolation and in combination with other components.
