# TargetTagRequirementsEffectComponent

> **Type:** `Gamesmiths.Forge.Effects.Components.TargetTagRequirementsEffectComponent`
> **State:** Stateful — returns a new instance from `CreateInstance`
> **Applies to:** Any effect

Validates that a target meets tag requirements for effect application, and keeps managing the effect's state as the target's tags change afterwards.

## Constructor

```csharp
new TargetTagRequirementsEffectComponent(applicationTagRequirements, removalTagRequirements, ongoingTagRequirements)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| applicationTagRequirements | `TagRequirements?` | Must be met at the moment of application, or the effect never lands. |
| removalTagRequirements | `TagRequirements?` | Once met, the effect is removed. |
| ongoingTagRequirements | `TagRequirements?` | While unmet, the effect is inhibited; it resumes when met again. |

All three are optional; an omitted or empty requirement is simply not evaluated.

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| `CanApplyEffect` | Rejects the application if the application requirements are unmet, or the removal requirements are already met. |
| `OnActiveEffectAdded` | Subscribes to the target's `OnTagsChanged` and reports the initial inhibition state. |
| `OnActiveEffectUnapplied` | Unsubscribes when the effect is fully removed. |

## Behavior

The component is **reactive**: it re-evaluates on every tag change on the target, not just at application. On each change it removes the effect if the removal requirements are now met, otherwise toggles inhibition from the ongoing requirements.

### The TagRequirements System

`TagRequirements` is the shared mechanism for evaluating tag conditions on entities.

```csharp
 public readonly record struct TagRequirements(
     TagContainer? RequiredTags = null,
     TagContainer? IgnoreTags = null,
     TagQuery? TagQuery = null)
{
    // Implementation...
}
```

- **RequiredTags**: tags that must all be present on the target.
- **IgnoreTags**: tags that must not be present on the target (any match fails).
- **TagQuery**: a query expression for advanced matching.

#### How TagRequirements Are Evaluated

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

#### Tag Query Usage

Tag queries allow for more complex expressions than simple "has all" and "has none" logic. See the [Tags documentation](../../tags.md) for more on tag queries.

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

## Validation

Ongoing requirements are rejected on an instant effect, since inhibition acts on an active effect and instant effects never become one.

## Usage

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

## Key Points

- Dynamically monitors tag changes on the target.
- Can prevent application, force removal, or toggle inhibition.
- Automatically cleans up event subscriptions when the effect is removed.
- Uses `TagRequirements` to define complex tag conditions.
- Tag-driven removal always removes **all** stacks.
- On an instant effect only `applicationTagRequirements` is fully meaningful. Ongoing requirements are rejected outright; removal requirements still deny application when already met, but nothing can be removed afterwards.

## See Also

- [Effect Components Overview](README.md)
- [Tags](../../tags.md)
- [ModifierTagsEffectComponent](modifier-tags-effect-component.md)
- [SourceTagRequirementsEffectComponent](source-tag-requirements-effect-component.md)
- [AttributeRequirementsEffectComponent](attribute-requirements-effect-component.md)
