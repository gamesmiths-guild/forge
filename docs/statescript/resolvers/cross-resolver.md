# CrossResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CrossResolver`
> **Output Type:** `Vector3`

Computes the cross product of two `Vector3` operands. Returns a `Vector3` that is perpendicular to both input vectors. Only `Vector3` operands are supported.

## Constructor

```csharp
new CrossResolver(left, right)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| left | `IPropertyResolver` | The resolver for the left vector operand. |
| right | `IPropertyResolver` | The resolver for the right vector operand. |

## Supported Types

| Operand Types | Result Type |
|---------------|-------------|
| `Vector3`, `Vector3` | `Vector3` |

**Invalid combinations** (throw `ArgumentException` at construction time):
- Any type other than `Vector3` (including `Vector2`, `Vector4`).
- Scalar types, `Quaternion`.
- Unsupported types (`bool`, `char`).

## Behavior

- Resolves both operands through their respective `IPropertyResolver` instances.
- Delegates to `Vector3.Cross(left, right)`.
- The result is perpendicular to both input vectors, with its direction determined by the right-hand rule.
- The magnitude of the result equals `|left| * |right| * sin(θ)`, where `θ` is the angle between the vectors.
- Cross product is anti-commutative: `Cross(a, b) = -Cross(b, a)`.
- Type validation happens at construction time (fail-fast), not at runtime.

## Usage

```csharp
// Compute the surface normal from two edge vectors
graph.VariableDefinitions.DefineProperty("surfaceNormal",
    new NormalizeResolver(
        new CrossResolver(
            new VariableResolver("edgeA", typeof(Vector3)),
            new VariableResolver("edgeB", typeof(Vector3)))));
```

## Composition

```csharp
// Compute an "up" vector from forward and right directions
graph.VariableDefinitions.DefineProperty("upVector",
    new NormalizeResolver(
        new CrossResolver(
            new VariableResolver("forward", typeof(Vector3)),
            new VariableResolver("right", typeof(Vector3)))));
```

## See Also

- [Resolvers Overview](README.md)
- [DotResolver](dot-resolver.md)
- [NormalizeResolver](normalize-resolver.md)
