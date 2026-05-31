# AbilityLevelResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityLevelResolver`

Returns the current ability level from the active `AbilityBehaviorContext`.

## Output

- `int`

## Behavior

1. Reads the current graph activation context.
2. If the graph is running inside an ability behavior, returns `AbilityBehaviorContext.Level`.
3. If no ability activation context exists, returns the default `int` value (`0`).

## Usage

```csharp
graph.VariableDefinitions.DefineProperty(
    "level",
    new AbilityLevelResolver());
```

## See Also

- [AbilityOwnershipResolver](ability-ownership-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
