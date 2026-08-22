# IsValidResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.IsValidResolver`
> **Output Type:** `bool`

Checks whether a nested object-backed resolver produces a usable value. Use it to validate object variables (entities, effects, handles) in condition nodes before acting on them, e.g. "is the stored target still set?".

## Constructor

```csharp
new IsValidResolver(source)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| source | `IObjectResolver` | The object-backed resolver whose result is checked. |

## Behavior

- Resolves `source` and returns `true` when the result is usable.
- A `null` result is never usable. Missing variables resolve to `null` and are therefore reported as invalid.
- A result implementing [`IValidatable`](#validatable-types) is usable only when its own `IsValid` is `true`. This matters because such values stay non-`null` after they stop referring to anything.
- Any other non-`null` result is usable.
- For an "is null" check, wrap this resolver in a [NotResolver](not-resolver.md) or, when driving an `ExpressionNode`, simply connect the `false` port.

## Validatable types

A handle is still a perfectly good reference after the thing it points at is gone, so a null check alone would call it valid and everything downstream would act on nothing. `Gamesmiths.Forge.Core.IValidatable` lets a type answer for itself, and these built-in types implement it:

| Type | Invalid when |
|------|--------------|
| `ActiveEffectHandle` | The effect has been removed. |
| `AbilityHandle` | The ability has been removed from the entity. |
| `AbilityInstanceHandle` | The ability instance has ended. |
| `Tag` | The tag is `Tag.Empty`. |

Implement `IValidatable` on your own types to have them checked the same way:

```csharp
public sealed class TargetLock : IValidatable
{
    public IForgeEntity? Target { get; set; }

    public bool IsValid => Target is not null;
}
```

> **Note:** engine integrations can extend the check further by overriding the `protected virtual bool IsUsable(object?)` method. The Godot integration does this to also reject objects that have been freed, which are neither `null` nor `IValidatable`.

## Usage

```csharp
new IsValidResolver(new EntityVariableResolver("storedTarget"))              // true when set
new NotResolver(new IsValidResolver(new EntityVariableResolver("storedTarget")))  // true when null

// An effect that has since been removed reports false, not true
new IsValidResolver(new ObjectVariableResolver<ActiveEffectHandle>("appliedBuff"))
```

## Composition

```csharp
// Gate a branch on the stored target being valid
graph.VariableDefinitions.DefineProperty(
    "hasTarget",
    new IsValidResolver(new EntityVariableResolver("storedTarget")));

// Or drop null entries from a reference array
new ObjectWhereResolver<IForgeEntity>(
    new EntityArrayVariableResolver("targets"),
    new IsValidResolver(new ElementResolver<IForgeEntity>()));
```

## See Also

- [Resolvers Overview](README.md)
- [NotResolver](not-resolver.md)
- [ObjectEqualsResolver](object-equals-resolver.md)
- [EntityVariableResolver](entity-variable-resolver.md)
