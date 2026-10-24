# InputAimResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.InputAimResolver`
> **Output Type:** `Vector3` or `Vector2`

Resolves where an entity's [input](../../input.md) aims: the point aimed at, or the direction aimed in.

## Constructor

```csharp
new InputAimResolver(value)
new InputAimResolver(inputSource, value)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| inputSource | `IEntityResolver` | Selects the entity whose input is read. Defaults to `AbilityOwnerResolver`. |
| value | `InputAimValue` | `Point3D`, `Point2D`, `Direction3D` or `Direction2D`. The 2D values resolve a `Vector2`. |

## Behavior

- The aim is the one whatever collected the input found, such as the ground under the mouse, rounded as it was collected. Every machine simulating the entity reads the same aim.
- A 2D game's aim has a Z of zero, and `Point2D` and `Direction2D` resolve its X and Y.
- Returns zero when the entity has no input, or its input carries no aim.

## Usage

```csharp
graph.VariableDefinitions.DefineProperty("aimPoint",
    new InputAimResolver(InputAimValue.Point3D));
```

## See Also

- [Resolvers Overview](README.md#input-resolvers)
- [Input](../../input.md)
