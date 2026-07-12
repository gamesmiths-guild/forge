# ActiveEffectEffectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ActiveEffectEffectResolver`
> **Output Type:** `Effect?`

Reads the live `Effect` instance behind an `ActiveEffectHandle` produced by a nested resolver, bridging the handle lane back to the effect lane, for example to write SetByCaller magnitudes or change the level of an effect you only hold a handle for.

## Constructor

```csharp
new ActiveEffectEffectResolver(handleResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| handleResolver | `IObjectResolver<ActiveEffectHandle>` | Produces the active effect handle to inspect. |

## Behavior

- Resolves the handle and returns its `Effect`. Invalid or missing handles resolve to `null`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<ActiveEffectHandle>("buff");

graph.VariableDefinitions.DefineObjectProperty("buffEffect",
    new ActiveEffectEffectResolver(new ObjectVariableResolver<ActiveEffectHandle>("buff")));
```

## Composition

```csharp
// Bridge a handle back to the effect lane, then level it up mid-flight
var effectLevel = new SetEffectLevelNode(); // operation: LevelUp
effectLevel.BindInput(SetEffectLevelNode.EffectInput, "buffEffect");
```

## See Also

- [Resolvers Overview](README.md)
- [SetByCallerMagnitudeNode](../nodes/action/set-by-caller-magnitude-node.md)
- [SetEffectLevelNode](../nodes/action/set-effect-level-node.md)
