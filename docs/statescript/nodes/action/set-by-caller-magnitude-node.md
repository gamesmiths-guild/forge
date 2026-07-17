# SetByCallerMagnitudeNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SetByCallerMagnitudeNode`

Sets a SetByCaller magnitude on one or more `Effect` instances, keyed by tag.

Setting a magnitude on an effect that has not been applied yet configures the value its `SetByCallerFloat` magnitudes read on application. Setting it on an effect that is already active live-updates non-snapshot SetByCaller magnitudes.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the change. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the change. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Effect | `Effect` or `Effect[]` | The effect instance(s) to write to. |
| 1 | Tag | `Tag` or `Tag[]` | The SetByCaller identifier tag(s). |
| 2 | Magnitude | `double` | The magnitude to set (cast to `float`). |

## Behavior

1. Resolves the effect(s), tag(s), and magnitude.
2. Writes the magnitude for every tag on every effect via `Effect.SetSetByCallerMagnitude(tag, magnitude)`.

> **Declarative alternative:** to guarantee a value is present before the *first* application (SetByCaller magnitude evaluation throws if the tag was never set), configure the magnitudes directly on the [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md) that builds the effect, instead of using this node.

> **Shared instances:** effects held in variables are shared instances. Writing their SetByCaller values affects every future application of that instance.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("damageTag",
    Tag.RequestTag(tagsManager, "data.damage"));
graph.VariableDefinitions.DefineVariable("damage", 25.0);

var setMagnitude = new SetByCallerMagnitudeNode();
setMagnitude.BindInput(SetByCallerMagnitudeNode.EffectInput, "damageEffect");
setMagnitude.BindInput(SetByCallerMagnitudeNode.TagInput, "damageTag");
setMagnitude.BindInput(SetByCallerMagnitudeNode.MagnitudeInput, "damage");
```

## See Also

- [Action Nodes Overview](README.md)
- [EffectFromDataResolver](../../resolvers/effect-from-data-resolver.md)
- [SetByCallerMagnitudeResolver](../../resolvers/set-by-caller-magnitude-resolver.md)
