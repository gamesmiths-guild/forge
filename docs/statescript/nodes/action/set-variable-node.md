# SetVariableNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode`

Reads a value from an input property and writes it to a graph or shared variable. This is the primary way to copy data between variables or write computed values.

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after execution. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Source | `Variant128` | The value to read. |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| 0 | Target | `Variant128` | The variable to write to. Scope (Graph or Shared) is configured via `BindOutput`. |

## Behavior

1. The node resolves the source input property as a raw `Variant128`.
2. If the source cannot be resolved, the node does nothing (the target variable is not modified).
3. The resolved value is written to the bound output variable.
4. If the output scope is `Shared`, the value is written to `GraphContext.SharedVariables`; otherwise, it is written to `GraphContext.GraphVariables`.
5. The output port emits a message.

## Usage

```csharp
var setVar = new SetVariableNode();
setVar.BindInput(SetVariableNode.SourceInput, "sourceProperty");
setVar.BindOutput(SetVariableNode.TargetOutput, "targetVariable");

// Write to a shared variable instead
setVar.BindOutput(SetVariableNode.TargetOutput, "comboCounter", VariableScope.Shared);
```

## See Also

- [Action Nodes Overview](README.md)
- [Variables and Data](../../variables.md)
