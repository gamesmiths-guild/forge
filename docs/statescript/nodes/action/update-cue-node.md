# UpdateCueNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.UpdateCueNode`

Updates one or more already-active cues (`CuesManager.UpdateCue`) on one or more targets, then continues execution. Use it to push new values to cues applied by a [CueNode](../state/cue-node.md) without re-applying them.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the cues are updated. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Cue Tags | `Tag` or `Tag[]` | The cue tag(s) to update. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities the cue plays on. |
| 2 | Magnitude | `int` | Optional. Cue magnitude. |
| 3 | Normalized Magnitude | `float` | Optional. Cue magnitude normalized to 0–1. |
| 4 | Source | `IForgeEntity` | Optional. The source entity carried in the cue parameters. |
| 5 | Custom Parameters | `Dictionary<StringKey, object>` | Optional. Custom parameter bag built by an `ICueCustomParametersProvider` (see [CueCustomParametersResolver](../../resolvers/cue-custom-parameters-resolver.md)). |

This node has no output variables, cues are addressed entirely by tag.

## Behavior

Identical resolution to [ExecuteCueNode](execute-cue-node.md): every cue tag is updated on every target (the `cueTag[] x target[]` matrix) through each target's `IForgeEntity.CuesManager`, with the optional parameters resolved once and shared. The only difference is that it calls `CuesManager.UpdateCue` instead of `ExecuteCue`, so the cue handler's `OnUpdate` runs rather than `OnExecute`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Tag>("chargeCue", Tag.RequestTag(tagsManager, "cue.charge"));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
graph.VariableDefinitions.DefineVariable("charge", 50);

var updateCue = new UpdateCueNode();
updateCue.BindInput(UpdateCueNode.CueTagInput, "chargeCue");
updateCue.BindInput(UpdateCueNode.TargetInput, "target");
updateCue.BindInput(UpdateCueNode.MagnitudeInput, "charge");
```

## See Also

- [Action Nodes Overview](README.md)
- [ExecuteCueNode](execute-cue-node.md)
- [CueNode](../state/cue-node.md)
- [CueCustomParametersResolver](../../resolvers/cue-custom-parameters-resolver.md)
