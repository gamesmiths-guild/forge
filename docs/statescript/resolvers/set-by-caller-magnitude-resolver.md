# SetByCallerMagnitudeResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.SetByCallerMagnitudeResolver`
> **Output Type:** `float`

Reads the SetByCaller magnitude currently stored on an `Effect` for a given identifier tag. This is the read-back companion of the [SetByCallerMagnitudeNode](../nodes/action/set-by-caller-magnitude-node.md).

## Constructor

```csharp
new SetByCallerMagnitudeResolver(effectResolver, identifierTag)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| effectResolver | `IObjectResolver<Effect>` | Produces the effect to inspect. |
| identifierTag | `Tag` | The SetByCaller identifier tag to read. |

## Behavior

- Resolves the effect and reads `Effect.DataTag[tag]`. Missing effects, or tags that were never set, resolve to `0`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable("damageEffect", damageEffectInstance);

graph.VariableDefinitions.DefineProperty("currentDamage",
    new SetByCallerMagnitudeResolver(
        new ObjectVariableResolver<Effect>("damageEffect"),
        Tag.RequestTag(tagsManager, "data.damage")));
```

## Composition

```csharp
// Show the configured damage in a UI-facing comparison
graph.VariableDefinitions.DefineProperty("isHeavyHit",
    new ComparisonResolver(
        new SetByCallerMagnitudeResolver(
            new ObjectVariableResolver<Effect>("damageEffect"),
            Tag.RequestTag(tagsManager, "data.damage")),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50f), typeof(float))));
```

## See Also

- [Resolvers Overview](README.md)
- [SetByCallerMagnitudeNode](../nodes/action/set-by-caller-magnitude-node.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
