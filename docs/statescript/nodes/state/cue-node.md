# CueNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.CueNode`
> **Context:** `CueNodeContext`

Applies one or more persistent cues (`CuesManager.ApplyCue`) on activation and removes them (`CuesManager.RemoveCue`) on deactivation.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Activates the state node. |
| 1 | Abort | Forcefully deactivates and fires OnAbort. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | OnActivate | Event | Emits when the node activates. |
| 1 | OnDeactivate | Event | Emits when the node deactivates (any reason). |
| 2 | OnAbort | Event | Emits only when aborted via the Abort port. |
| 3 | Subgraph | Subgraph | Remains active while the node is active; sends disable signal on deactivation. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Cue Tags | `Tag` or `Tag[]` | The cue tag(s) to apply while active. |
| 1 | Target | `IForgeEntity` or `IForgeEntity[]` | The entity or entities the cue plays on. |
| 2 | Magnitude | `int` | Optional. Cue magnitude for the apply. |
| 3 | Normalized Magnitude | `float` | Optional. Cue magnitude normalized to 0–1. |
| 4 | Source | `IForgeEntity` | Optional. The source entity carried in the cue parameters. |

This node has no output variables, cues are addressed entirely by tag.

## Behavior

1. On activation, the node resolves the **Cue Tags** and **Target** inputs (single or array each) and applies every cue tag to every target, the full `cueTag[] x target[]` matrix, through each target's `IForgeEntity.CuesManager`. The optional **Magnitude** / **Normalized Magnitude** / **Source** inputs are resolved once and shared (`null` parameters when all are unbound).
2. The exact cue/target pairs applied on activation are recorded in `CueNodeContext`.
3. The node has **no timer**: it stays active until deactivated externally. It is typically placed as a subgraph of another state node so it lives for that state's duration.
4. On deactivation, the node removes exactly the recorded cue/target pairs. Whether the removal is an interruption is derived from how the node was deactivated: a natural shutdown (parent subgraph ending or `GraphProcessor.StopGraph`) passes `interrupted: false`, while a deactivation forced through the **Abort** port passes `interrupted: true`. No interrupt input is needed.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Tag>("auraCue", Tag.RequestTag(tagsManager, "cue.aura"));
graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

var cueNode = new CueNode();
cueNode.BindInput(CueNode.CueTagInput, "auraCue");
cueNode.BindInput(CueNode.TargetInput, "target");
```

Place the node as a subgraph of another state node so it lives for that state's duration; route an interrupting signal into its **Abort** port when a removal should be treated as an interruption.

## See Also

- [State Nodes Overview](README.md)
- [ExecuteCueNode](../action/execute-cue-node.md)
- [UpdateCueNode](../action/update-cue-node.md)
