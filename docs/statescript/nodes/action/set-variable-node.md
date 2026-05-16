# SetVariableNode

> **Type:** Action Node
> **Class:** `Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode`

Reads a value from an input property and writes it to an existing graph or shared variable. This is the primary way to copy data between variables or write computed values.

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

1. The node selects the target variable bag from the bound output scope (`GraphContext.GraphVariables` or `GraphContext.SharedVariables`).
2. It inspects the bound target variable to determine its storage kind: `Variant128`, `Variant128[]`, reference value, or reference array.
3. That target binding is cached for the current graph execution and reused on later `Execute` calls while the node writes to the same variable bag.
4. The source input is resolved using the API that matches the target kind.
5. If the source cannot be resolved, the node does nothing (the target variable is not modified).
6. The resolved value is written to the bound output variable, preserving the target variable's declared kind.
7. The output port emits a message.

## Notes

- The target variable must already exist in the selected scope. `SetVariableNode` does not create missing target variables.
- The declared target variable kind controls how the source is read and written. This lets the same node work with value variables, arrays, reference variables, and reference arrays.

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
