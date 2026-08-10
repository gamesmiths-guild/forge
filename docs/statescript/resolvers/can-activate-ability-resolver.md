# CanActivateAbilityResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CanActivateAbilityResolver`
> **Output Type:** `bool`

Checks whether an ability can currently activate, covering cooldowns, costs, tag requirements, and blocking, exactly like activating it would. By default it reads the ability driving the current graph.

## Constructor

```csharp
new CanActivateAbilityResolver(targetResolver = null, handleResolver = null)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| targetResolver | `IEntityResolver?` | The activation target used by target tag requirement checks. |
| handleResolver | `IObjectResolver<AbilityHandle>?` | The ability to inspect. Defaults to the graph's ability. |

## Behavior

- Calls `AbilityHandle.CanActivate(out _, target)`.
- Missing abilities resolve to `false`.

## Usage

```csharp
// Only commit if the ability can actually activate
graph.VariableDefinitions.DefineProperty("canCast", new CanActivateAbilityResolver());

var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "canCast");
```

## Composition

```csharp
// Gate a proc on both a random roll and the other ability being ready
graph.VariableDefinitions.DefineProperty("shouldProc",
    new AndResolver(
        new CanActivateAbilityResolver(
            handleResolver: new GetAbilityHandleResolver(comboAbilityData)),
        new ComparisonResolver(
            new RandomResolver(
                randomProvider,
                new VariantResolver(new Variant128(0f), typeof(float)),
                new VariantResolver(new Variant128(1f), typeof(float))),
            ComparisonOperation.LessThan,
            new VariantResolver(new Variant128(0.3f), typeof(float)))));
```

## See Also

- [Resolvers Overview](README.md)
- [TryCommitAbilityNode](../nodes/condition/try-commit-ability-node.md)
- [AbilityCooldownResolver](ability-cooldown-resolver.md)
