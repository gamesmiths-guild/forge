# AbilityCostResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityCostResolver`
> **Output Type:** `int`

Reads the evaluated cost of an ability for a specific attribute. By default it reads the ability driving the current graph; provide an `IObjectResolver<AbilityHandle>` to inspect a different ability.

The cost is the evaluated modifier value of the ability's cost effect, so a mana cost of 5 resolves as `-5`.

## Constructor

```csharp
new AbilityCostResolver(attributeKey, handleResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| attributeKey | `StringKey` | The fully qualified attribute key to read the cost for. |
| handleResolver | `IObjectResolver<AbilityHandle>?` | The ability to inspect. Defaults to the graph's ability. |

## Behavior

- Reads `AbilityHandle.GetCostForAttribute(attributeKey)`.
- Missing abilities, or attributes without a cost, resolve to `0`.

## Usage

```csharp
// The current ability's mana cost (negative, e.g. -5)
graph.VariableDefinitions.DefineProperty("manaCost",
    new AbilityCostResolver("CombatAttributeSet.Mana"));
```

## Composition

```csharp
// Warn when the owner cannot afford the ability's mana cost
graph.VariableDefinitions.DefineProperty("cannotAfford",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Mana"),
        ComparisonOperation.LessThan,
        new NegateResolver(new AbilityCostResolver("CombatAttributeSet.Mana"))));
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityCooldownResolver](ability-cooldown-resolver.md)
- [CanActivateAbilityResolver](can-activate-ability-resolver.md)
