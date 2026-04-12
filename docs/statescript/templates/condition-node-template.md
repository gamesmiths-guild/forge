# {NodeName}

> **Type:** Condition Node
> **Class:** `{Namespace}.{ClassName}`

{Brief one-paragraph description of what this condition node evaluates.}

## Ports

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits if the test returns `true`. |
| 1 | False | Event | Emits if the test returns `false`. |

## Parameters

**Input Properties:**

| Index | Label | Type | Description |
|-------|-------|------|-------------|
| {index} | {Label} | `{Type}` | {Description.} |

> Remove the Input Properties table if the node has none.

## Behavior

{Describe the evaluation logic:}

1. {What the node tests.}
2. {When it routes to True vs False.}

## Usage

```csharp
// {Example showing node creation, binding, and wiring}
```

## See Also

- [Condition Nodes Overview](README.md)
- [{Related node or resolver}]({relative-link})
