# Effect Components

Effect Components in Forge allows developers to extend effect functionality through a modular, composable approach. Components can add custom behaviors, validation logic, and react to different events in an effect's lifecycle.

For a practical guide on using components, see the [Quick Start Guide](../../quick-start.md). For how components fit into effects as a whole, see the [Effects overview](../README.md).

---

## Built-in Components

| Component | State | Applies to | Description |
|-----------|-------|------------|-------------|
| [BlockAbilityTagsEffectComponent](block-ability-tags-effect-component.md) | Stateful | Duration | Blocks abilities carrying the given tags from activating while the effect is active. |
| [CancelAbilityTagsEffectComponent](cancel-ability-tags-effect-component.md) | Stateless | Any | Cancels active abilities selected by tag, on application or on each execution. |
| [ChanceToApplyEffectComponent](chance-to-apply-effect-component.md) | Stateless | Any | Gives the effect a random chance to apply, optionally scaling with level. |
| [GrantAbilityEffectComponent](grant-ability-effect-component.md) | Stateful | Any | Grants abilities for the effect's lifetime, or permanently from an instant effect. |
| [ModifierTagsEffectComponent](modifier-tags-effect-component.md) | Stateless | Duration | Adds tags to the target while the effect is active. |
| [TargetTagRequirementsEffectComponent](target-tag-requirements-effect-component.md) | Stateful | Any | Gates application, forces removal, and toggles inhibition from the target's tags. |

To add a new component page, copy [effect-component-template.md](../../templates/effect-component-template.md) and add a row above.

---

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
    IEffectComponent CreateInstance();
    bool CanApplyEffect(in IForgeEntity target, in Effect effect);
    bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData);
    void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData);
    void OnActiveEffectUnapplied(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData, bool removed, EffectRemovalReason reason);
    void OnActiveEffectChanged(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData);
    void OnEffectApplied(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData);
    void OnEffectExecuted(IForgeEntity target, in EffectEvaluatedData effectEvaluatedData);
}
```

The interface provides default implementations for all methods, so you only need to override the ones relevant to your component's functionality.

### Component Lifecycle Methods

#### CreateInstance

`CreateInstance` is called when the effect is applied and allows the component to provide either a shared (stateless) instance or return a new instance for per-application (stateful) data.

Override this method when your component holds data that must be isolated per-effect application, such as event subscriptions or runtime counters.

```csharp
public class ExampleComponent : IEffectComponent
{
    private int _someState;

    public IEffectComponent CreateInstance()
    {
        // Return a new instance so each effect application has its own state
        return new ExampleComponent();
    }
}
```

Use cases:

- Tracking data or resources that must not be shared across multiple effect instances.
- Managing event subscriptions or references tied to a specific application of an effect.
- Ensuring thread safety or isolation when effects are applied to different targets simultaneously.

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

#### OnPostActiveEffectAdded

`OnPostActiveEffectAdded` is called after all components’ `OnActiveEffectAdded` callbacks have completed, and the effect has finished its initial application logic. At this point, the effect is fully initialized.

Override this method to perform actions that rely on other components being initialized, or when you need to trigger behaviors that should occur after the effect is completely active.

```csharp
public void OnPostActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
{
    // Logic here runs after all initialization and validation is complete
    // For example, attempt activation if not inhibited
    if (!activeEffectEvaluatedData.ActiveEffectHandle.IsInhibited)
    {
        // Custom post-activation logic
    }
}
```

This is also the earliest point at which `ActiveEffectHandle.IsInhibited` is settled, so any component whose initial behavior depends on inhibition must decide here rather than in `OnActiveEffectAdded`.

Use cases:

- Conditionally activating abilities granted by earlier components.
- Synchronizing with other components after full effect application.
- Triggering animations, particles, or gameplay effects that should be delayed until the effect is stable.

#### OnActiveEffectUnapplied

Called when an effect is unapplied or a stack is removed.

```csharp
public void OnActiveEffectUnapplied(
    IForgeEntity target,
    in ActiveEffectEvaluatedData activeEffectEvaluatedData,
    bool removed,
    EffectRemovalReason reason)
{
    // Custom cleanup logic
    if (removed) {
        // Effect was completely removed
    } else {
        // Just a stack was removed
    }

    if (reason == EffectRemovalReason.Expired) {
        // The effect ran its course
    } else {
        // The effect was taken away early
    }
}
```

`reason` distinguishes an effect that ended on its own from one that was taken away:

- `EffectRemovalReason.Expired` — the effect ran out of duration. Only `HasDuration` effects can expire.
- `EffectRemovalReason.Removed` — the effect was removed through one of the `EffectsManager` removal methods before it could expire. Since `Infinite` effects have no natural end, every removal of one reports `Removed`.

The same value drives the `interrupted` flag that cue handlers receive in `ICueHandler.OnRemove`.

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

Because this fires for every one of those reasons, a component reacting to only one of them — inhibition, typically — has to compare against its own tracked state rather than assuming the callback means what it wants.

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
    private readonly float _threshold;
    private readonly Effect _thresholdEffect;
    private float _accumulatedDamage;
    private EventSubscriptionToken? _damageEventToken;

    public DamageThresholdComponent(float threshold, Effect thresholdEffect)
    {
        _threshold = threshold;
        _thresholdEffect = thresholdEffect;
    }

    // Guarantees each effect application has its own unique instance and state
    public IEffectComponent CreateInstance()
    {
        return new DamageThresholdComponent(_threshold, _thresholdEffect);
    }

    public bool OnActiveEffectAdded(IForgeEntity target, in ActiveEffectEvaluatedData activeEffectEvaluatedData)
    {
        _accumulatedDamage = 0f;
        // Subscribe to an "events.combat.damage_taken" event using Forge's Events system
        var damageTakenTag = Tag.RequestTag(target.TagsManager, "events.combat.damage_taken");
        _damageEventToken = target.Events.Subscribe(damageTakenTag, data =>
        {
            _accumulatedDamage += data.EventMagnitude;
            if (_accumulatedDamage >= _threshold)
            {
                _accumulatedDamage = 0;
                target.EffectsManager.ApplyEffect(_thresholdEffect);
            }
        });
        return true;
    }

    public void OnActiveEffectUnapplied(
        IForgeEntity target,
        in ActiveEffectEvaluatedData activeEffectEvaluatedData,
        bool removed,
        EffectRemovalReason reason)
    {
        if (removed && _damageEventToken is not null)
        {
            target.Events.Unsubscribe(_damageEventToken.Value);
            _damageEventToken = null;
        }
    }
}
```

Once written, document it: copy [effect-component-template.md](../../templates/effect-component-template.md), fill it in, and add a row to the [Built-in Components](#built-in-components) table.

### Accessing Component Instances at Runtime

When you apply a duration (non-instant) effect, you receive an `ActiveEffectHandle` from the `EffectsManager`. This handle provides access to the specific component instances that were created for this effect application.

This is useful if you need to check runtime state, interact with a component that manages resources, or access data (such as granted abilities or custom counters) unique to this particular effect instance.

#### Retrieving a Component Instance

You can retrieve a component instance of a given type using the handle's generic `GetComponent<T>()` method:

```csharp
// Apply an effect and get the handle.
ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(new Effect(effectData, ownership));

if (handle is not null)
{
    // Retrieve a specific component instance used by this effect.
    var grantAbilityComponent = handle.GetComponent<GrantAbilityEffectComponent>();
    if (grantAbilityComponent is not null)
    {
        // Access runtime data exposed by the component
        IReadOnlyList<AbilityHandle> grantedAbilities = grantAbilityComponent.GrantedAbilities;
        // ... use grantedAbilities as needed
    }

    // You can also enumerate all component instances for additional logic
    foreach (var component in handle.ComponentInstances)
    {
        // Inspect or interact with component instances
    }
}
```

- `GetComponent<T>()` returns the first component instance of type `T` (or `null` if none exists).
- `ComponentInstances` exposes all component instances for this effect (may hold per-instance state).

**Typical use cases:**
- Accessing granted ability handles from a `GrantAbilityEffectComponent`.
- Inspecting or updating internal state on a custom component.
- Coordinating follow-up logic or queries in gameplay systems.

For more details on the structure of `ActiveEffectHandle`, see the [ActiveEffectHandle documentation](../README.md#activeeffecthandle).

### Advanced Component Integration

Components can be used to implement complex systems that integrate with your game's mechanics:

- **Combat Reaction System**: Components that trigger reactions between elements.
- **Cooldown Management**: Components that track and enforce cooldowns between effect applications.
- **Cross-Effect Coordination**: Components that coordinate between multiple active effects.
- **Attribute Threshold Monitoring**: Components that trigger effects when attributes cross thresholds.
- **AI Behavior Modification**: Components that adjust AI behavior when effects are active.

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
