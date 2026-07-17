# ConditionalObjectResolver&lt;T&gt;

> **Type:** `Gamesmiths.Forge.Statescript.Properties.ConditionalObjectResolver<T>`
> **Output Type:** `T?` *(matches the branches)*

Selects one of two object-lane values based on a boolean condition — the ternary select for reference values (for example, picking between two entities). Only the selected branch is evaluated.

## Constructor

```csharp
new ConditionalObjectResolver<T>(condition, whenTrue, whenFalse)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| condition | `IPropertyResolver` | The boolean condition. |
| whenTrue | `IObjectResolver<T>` | Evaluated when the condition is `true`. |
| whenFalse | `IObjectResolver<T>` | Evaluated when the condition is `false`. |

## Behavior

- Evaluates the condition, then resolves and returns only the selected object branch.

## Usage

```csharp
// Target the ally when "supporting", otherwise the enemy
new ConditionalObjectResolver<IForgeEntity>(
    new TagQueryResolver(supportingQuery),
    new EntityVariableResolver("ally"),
    new AbilityTargetResolver());
```

## Composition

```csharp
// Read an attribute from whichever entity the condition selects
graph.VariableDefinitions.DefineProperty("selectedHealth",
    new AttributeResolver(
        "CombatAttributeSet.Health",
        new ConditionalObjectResolver<IForgeEntity>(
            new TagQueryResolver(supportingQuery),
            new EntityVariableResolver("ally"),
            new AbilityTargetResolver())));
```

## See Also

- [Resolvers Overview](README.md)
- [ConditionalResolver](conditional-resolver.md)
