# OwnershipResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.OwnershipResolver`
> **Output Type:** `EffectOwnership`

Composes an `EffectOwnership` value from nested entity resolvers.

## Constructor

```csharp
new OwnershipResolver(ownerResolver, sourceResolver)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| ownerResolver | `IEntityResolver?` | Resolver used for the resulting `EffectOwnership.Owner`. Optional. |
| sourceResolver | `IEntityResolver?` | Resolver used for the resulting `EffectOwnership.Source`. Optional. |

## Behavior

- Resolves the configured owner entity resolver, if one was provided.
- Resolves the configured source entity resolver, if one was provided.
- Returns a new `EffectOwnership` built from those resolved entities.
- Allows either side to resolve to `null`.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "ownership",
    new OwnershipResolver(
        new AbilityOwnerResolver(),
        new AbilitySourceResolver()));
```

## Composition

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "redirectedOwnership",
    new OwnershipResolver(
        new AbilityTargetResolver(),
        new AbilitySourceResolver()));
```

## See Also

- [AbilityOwnershipResolver](ability-ownership-resolver.md)
- [AbilityOwnerResolver](ability-owner-resolver.md)
- [AbilitySourceResolver](ability-source-resolver.md)
- [AbilityTargetResolver](ability-target-resolver.md)
