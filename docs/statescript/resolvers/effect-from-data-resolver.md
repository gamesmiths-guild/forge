# EffectFromDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectFromDataResolver`
> **Output Type:** `Effect`

Builds an `Effect` instance from an `EffectData` value plus optional level and ownership resolvers. This is the standard way to author the `Effect` input of [ApplyEffectNode](../nodes/action/apply-effect-node.md) and [EffectNode](../nodes/state/effect-node.md).

## Constructor

```csharp
new EffectFromDataResolver(effectData)
new EffectFromDataResolver(effectData, levelResolver)
new EffectFromDataResolver(effectData, levelResolver, ownershipResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData` | The effect configuration data used to build the effect. |
| levelResolver | `IPropertyResolver?` | Optional resolver for the effect level. |
| ownershipResolver | `IObjectResolver<EffectOwnership>?` | Optional resolver for the effect ownership. |

## Behavior

- Resolves the level and ownership, then returns `new Effect(effectData, ownership, level)`.
- When no level resolver is provided, the level falls back to the active ability level, or `1` without an ability context.
- When no ownership resolver is provided, the ownership falls back to the active ability owner/source, or `null`/`null` without an ability context.
- Each resolve call builds a new `Effect` instance. To reuse a single instance, store the resolved effect in a variable (see [EffectVariableResolver](effect-variable-resolver.md)).

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "burnEffect",
    new EffectFromDataResolver(burnEffectData));

var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "burnEffect");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "target");
```

## Composition

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "burnEffect",
    new EffectFromDataResolver(
        burnEffectData,
        new AbilityLevelResolver(),
        new OwnershipResolver(new AbilityOwnerResolver(), new AbilitySourceResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [EffectArrayFromDataResolver](effect-array-from-data-resolver.md)
- [EffectVariableResolver](effect-variable-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
