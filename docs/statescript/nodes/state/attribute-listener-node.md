# AttributeListenerNode

> **Type:** State Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.State.AttributeListenerNode`
> **Context:** `AttributeListenerNodeContext`

Listens for value changes on an entity attribute while active, emitting an event with the new value and the change delta.

## Ports

Standard state ports, plus:

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 4 | OnChanged | Event | Emits each time the attribute's value changes. |

## Constructor

```csharp
new AttributeListenerNode(attributeKey)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attributeKey | `StringKey` | The fully qualified key of the attribute to observe (e.g. `"CombatAttributeSet.Health"`). |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Entity | `IForgeEntity` | Optional. The entity whose attribute is observed. Defaults to the ability context's owner. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | New Value | `int` | The attribute's new current value. |
| 1 | Delta | `int` | The change amount. |

## Behavior

1. On activation, subscribes to `EntityAttribute.OnValueChanged` for the configured attribute on the resolved entity.
2. On each change, writes **New Value** and **Delta**, then emits `OnChanged` synchronously.
3. Unsubscribes on deactivation.

## Usage

```csharp
var listener = new AttributeListenerNode("CombatAttributeSet.Health");
listener.BindOutput(AttributeListenerNode.NewValueOutput, "currentHealth");
listener.BindOutput(AttributeListenerNode.DeltaOutput, "healthDelta");
```

## See Also

- [State Nodes Overview](README.md)
- [AttributeResolver](../../resolvers/attribute-resolver.md)
- [ConditionMonitorNode](condition-monitor-node.md)
