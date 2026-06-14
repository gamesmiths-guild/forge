# ExecuteCueNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.ExecuteCueNode`

Executes one or more one-shot cues (`CuesManager.ExecuteCue`) on one or more targets, then continues execution.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the cues are fired. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Cue Tags | `Tag` or `Tag[]` | The cue tag(s) to execute. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities the cue plays on. |
| 2 | Magnitude | `int` | Optional. Cue magnitude. |
| 3 | Normalized Magnitude | `float` | Optional. Cue magnitude normalized to 0–1. |
| 4 | Source | `IForgeEntity` | Optional. The source entity carried in the cue parameters. |

This node has no output variables, cues are addressed entirely by tag.

## Behavior

1. The node resolves the **Cue Tags** input as a single `Tag` or an array of tags.
2. It resolves the **Target** input as a single `IForgeEntity` or an array of entities.
3. It fires every cue tag on every target, forming a full `cueTag[] x target[]` matrix. Each cue is executed through that target's `IForgeEntity.CuesManager`.
4. The optional **Magnitude**, **Normalized Magnitude**, and **Source** inputs are resolved once and shared across the matrix. When all three are unbound, the cues are executed with `null` parameters; otherwise a `CueParameters` is built from the resolved values (unbound fields default to `0` / `0` / `null`).
5. The output port emits after all cues are fired.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Tag>("hitCue", Tag.RequestTag(tagsManager, "cue.hit"));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

var executeCue = new ExecuteCueNode();
executeCue.BindInput(ExecuteCueNode.CueTagInput, "hitCue");
executeCue.BindInput(ExecuteCueNode.TargetInput, "target");
```

To pass parameters, bind any of the optional inputs:

```csharp
graph.VariableDefinitions.DefineVariable("magnitude", 25);
executeCue.BindInput(ExecuteCueNode.MagnitudeInput, "magnitude");
```

## See Also

- [Action Nodes Overview](README.md)
- [UpdateCueNode](update-cue-node.md)
- [CueNode](../state/cue-node.md)
