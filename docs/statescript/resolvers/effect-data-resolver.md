# EffectDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectDataResolver`
> **Output Type:** `EffectData`

Returns a fixed `EffectData` value from graph properties so effect-oriented nodes can apply a concrete effect definition.

## Constructor

```csharp
new EffectDataResolver(effectData)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData` | The effect definition to return whenever the property is resolved. |

## Behavior

- Returns the same `EffectData` value on every resolve call.
- Uses the strongly typed generic object-backed resolver path, so `EffectData` can participate in the same authoring
  APIs as reference-typed resolver values.
- Useful for authoring fixed effect properties in pure C# graph construction.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "burnEffect",
    new EffectDataResolver(burnEffectData));
```

```csharp
var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "burnEffect");
effectNode.BindInput(EffectNode.TargetInput, "target");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectDataArrayResolver](effect-data-array-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
