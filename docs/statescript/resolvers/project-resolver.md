# ProjectResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ProjectResolver`
> **Output Type:** `Vector2`, `Vector3`, or `Vector4`

Projects one vector onto another vector. Use this resolver when you need only the component of a vector that lies along a given direction.

## Constructor

```csharp
new ProjectResolver(value, onto)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| value | `IPropertyResolver` | The resolver for the source vector. |
| onto | `IPropertyResolver` | The resolver for the vector to project onto. |

## Supported Types

| Value Type | Onto Type | Result Type |
|------------|-----------|-------------|
| `Vector2` | `Vector2` | `Vector2` |
| `Vector3` | `Vector3` | `Vector3` |
| `Vector4` | `Vector4` | `Vector4` |

## Behavior

- Resolves both vector operands on each call.
- Requires both operands to have matching vector types.
- Returns the projection of `value` onto `onto`.
- Supports `Vector2`, `Vector3`, and `Vector4` operands only.
- Throws `ArgumentException` at construction time for mismatched or unsupported types.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("velocityAlongForward",
    new ProjectResolver(
        new VariableResolver("velocity", typeof(Vector3)),
        new VariableResolver("forward", typeof(Vector3))));
```

## Composition

```csharp
graph.VariableDefinitions.DefineProperty("lateralVelocity",
    new RejectResolver(
        new VariableResolver("velocity", typeof(Vector3)),
        new ProjectResolver(
            new VariableResolver("velocity", typeof(Vector3)),
            new VariableResolver("forward", typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [RejectResolver](reject-resolver.md)
- [DotResolver](dot-resolver.md)
