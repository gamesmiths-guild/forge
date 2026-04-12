# {NodeName}

> **Type:** State Node
> **Class:** `{Namespace}.{ClassName}`
> **Context:** `{ContextClassName}`

{Brief one-paragraph description of what this state node does and when it deactivates.}

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
| 3 | Subgraph | Subgraph | Active while the node is active; sends disable signal on deactivation. |
| {4+} | {Custom} | {Event or Subgraph} | {Description of additional ports.} |

> Remove the custom port rows if the node defines no additional ports beyond the standard four.

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| {index} | {Label} | `{Type}` | {Description.} |

**Output Variables:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| {index} | {Label} | `{Type}` | {Description.} |

> Remove the Input Properties or Output Variables table if the node has none.

## Behavior

{Describe the full lifecycle:}

1. **Activation:** {What happens on activation.}
2. **Update:** {What happens each frame while active.}
3. **Deactivation:** {What triggers deactivation and what cleanup occurs.}

## Usage

```csharp
// {Example showing node creation, binding, and wiring}
```

## See Also

- [State Nodes Overview](README.md)
- [{Related node or resolver}]({relative-link})
