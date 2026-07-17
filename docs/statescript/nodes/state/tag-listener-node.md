# TagListenerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.TagListenerNode`
> **Context:** `TagListenerNodeContext`

Listens for tag changes on an entity while active, emitting events when watched tags are added to or removed from the entity's tag view.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnTagAdded | Event | Emits when a watched tag is added. |
| 5 | OnTagRemoved | Event | Emits when a watched tag is removed. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Entity | `IForgeEntity` | Optional. The entity whose tags are observed. Defaults to the ability context's owner. |
| 1 | Tags | `Tag` or `Tag[]` | The tag(s) to watch. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Tag | `Tag` | The tag that changed (written before each emit). |

## Behavior

1. On activation, records the current presence of each watched tag and subscribes to the entity's tag-change event.
2. On each change, for every watched tag whose presence flipped, writes the **Tag** output and emits `OnTagAdded` or `OnTagRemoved`. Presence checks use the entity's full tag view (base + modifier) with hierarchical matching.
3. Unsubscribes on deactivation.

> For a polling alternative that does not require event support, pair a [ConditionMonitorNode](condition-monitor-node.md) with a [TagQueryResolver](../../resolvers/tag-query-resolver.md).

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("stunTag",
    Tag.RequestTag(tagsManager, "status.stunned"));
graph.VariableDefinitions.DefineObjectVariable<Tag>("changedTag");

var listener = new TagListenerNode();
listener.BindInput(TagListenerNode.TagInput, "stunTag");
listener.BindOutput(TagListenerNode.TagOutput, "changedTag");

graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    listener.InputPorts[StateNode<TagListenerNodeContext>.InputPort]));
graph.AddConnection(new Connection(
    listener.OutputPorts[TagListenerNode.OnTagAddedPort],
    onStunnedNode.InputPorts[ActionNode.InputPort]));
```

## See Also

- [State Nodes Overview](README.md)
- [TagQueryResolver](../../resolvers/tag-query-resolver.md)
- [ConditionMonitorNode](condition-monitor-node.md)
