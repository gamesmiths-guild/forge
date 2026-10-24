# InputActionNode

> **Type:** State Node
> **Network kind:** Simulation
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.InputActionNode`
> **Context:** `InputActionNodeContext`

Watches a button of an entity's [input](../../../input.md) while active, emitting press and release events and routing a held subgraph.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnPressed | Event | Emits when the button goes down. |
| 5 | OnReleased | Event | Emits when the button comes up. |
| 6 | WhilePressed | Subgraph | Active while the button is down. |

## Parameters

**Constructor:**

| Parameter | Type | Description |
|-----------|------|-------------|
| actionName | `string` | The input action to watch. |
| deactivateOnPressed | `bool` | Whether the node deactivates itself on the first press. Defaults to `false`. |

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Input Source | `IForgeEntity` | Optional. The entity whose input is watched. Defaults to the ability context's owner. |

## Behavior

1. Reads the input source entity's `EntityInput` when it activates, then on every fixed step, so it sees the frame of the tick it runs in.
2. **The first read is the starting state, not a change.** A button already held when the node activates starts WhilePressed but isn't a press. A combo window must not fire on a button nobody pressed inside it, and the button that started the ability is usually still down when the window opens.
3. After that, a press emits OnPressed and starts WhilePressed, and a release ends WhilePressed, then emits OnReleased.
4. **One tick can carry both edges.** The state before the tick decides their order: a button that was down was released first, and one that was up was pressed first. A tap shorter than a tick reports a press, then a release.
5. With `deactivateOnPressed`, the first press deactivates the node and emits OnPressed.
6. An entity without an input, or an input whose action set doesn't carry the action, is never read.

Common shapes:

- **Hold to channel:** the channel goes on WhilePressed and ends when the button comes up.
- **Charged shot:** a timer starts on OnPressed, and the shot fires on OnReleased.
- **Combo window:** inside a timer's subgraph, a node with `deactivateOnPressed` waits for one press.

## Usage

```csharp
var watch = new InputActionNode("fire");

graph.AddNode(watch);
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    watch.InputPorts[StateNode<InputActionNodeContext>.InputPort]));

// The charging effect lasts while the button is held, and the shot fires on release.
graph.AddConnection(new Connection(
    watch.OutputPorts[InputActionNode.WhilePressedPort],
    chargingEffect.InputPorts[StateNode<EffectNodeContext>.InputPort]));
graph.AddConnection(new Connection(
    watch.OutputPorts[InputActionNode.OnReleasedPort],
    applyShot.InputPorts[ActionNode.InputPort]));
```

## Reading a device

An engine integration can derive from the node to read its own input device when the input source has no input. `ResolveInputSource(graphContext)` returns the entity, and `ReadButtonState(graphContext, down)` reports a polled state: a change since the last read becomes a press or a release, and the first read is the starting state. Forge for Godot's Input Action node reads the local device this way, every frame.

## See Also

- [State Nodes Overview](README.md)
- [Input](../../../input.md)
- [InputActionResolver](../../resolvers/input-action-resolver.md)
