# EffectArrayFromDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectArrayFromDataResolver`
> **Output Type:** `Effect[]`

Builds an array of `Effect` instances from an array of `EffectData` values plus optional level and ownership resolvers, applying the same level and ownership to every produced effect.

## Constructor

```csharp
new EffectArrayFromDataResolver(effectData)
new EffectArrayFromDataResolver(effectData, levelResolver)
new EffectArrayFromDataResolver(effectData, levelResolver, ownershipResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData[]` | The effect configuration data values used to build the effects. |
| levelResolver | `IPropertyResolver?` | Optional resolver for the effect level shared by all produced effects. |
| ownershipResolver | `IObjectResolver<EffectOwnership>?` | Optional resolver for the effect ownership shared by all produced effects. |

## Behavior

- Builds one `Effect` per element with the resolved level and ownership.
- Level and ownership fall back to the active ability context using the same rules as [EffectFromDataResolver](effect-from-data-resolver.md).
- Useful for authoring the array form of the `Effect` input on `ApplyEffectNode` and `EffectNode`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty(
    "effects",
    new EffectArrayFromDataResolver([burnEffectData, slowEffectData]));

var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "effects");
effectNode.BindInput(EffectNode.TargetInput, "target");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
- [EffectArrayVariableResolver](effect-array-variable-resolver.md)
- [EffectNode](../nodes/state/effect-node.md)
