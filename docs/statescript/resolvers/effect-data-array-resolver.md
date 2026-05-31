# EffectDataArrayResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectDataArrayResolver`
> **Output Type:** `EffectData[]`

Returns a fixed array of `EffectData` values from graph properties so effect-oriented nodes can apply multiple effect
definitions from a single binding.

## Constructor

```csharp
new EffectDataArrayResolver(params EffectData[] effectData)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectData | `EffectData[]` | The effect definitions to return whenever the property is resolved. |

## Behavior

- Returns the same `EffectData[]` contents on every resolve call.
- Preserves the declared element order.
- Uses the strongly typed generic object-backed resolver path, so `EffectData[]` can participate in the same
  authoring APIs as reference-typed resolver arrays.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayProperty(
    "effects",
    new EffectDataArrayResolver(burnEffectData, slowEffectData));
```

```csharp
var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "effects");
effectNode.BindInput(EffectNode.EntityInput, "target");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectDataResolver](effect-data-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
