# Custom Resolvers

Property resolvers provide read-only computed values that nodes can bind to as input properties. Forge ships with several [built-in resolvers](variables.md#built-in-resolvers) (`AttributeResolver`, `TagResolver`, `ComparisonResolver`, `VariableResolver`, `SharedVariableResolver`, `VariantResolver`, `MagnitudeResolver`), but you can create your own to expose any data source to graph nodes without writing custom node subclasses.

For an overview of the Statescript system, see the [Statescript overview](README.md). For how resolvers fit into the broader data flow, see [Variables and Data](variables.md).

## When to Create a Custom Resolver

Use a custom resolver when you need to expose data that the built-in resolvers don't cover. Typical scenarios:

- **Game-specific state**: Time of day, weather intensity, wave number, difficulty multiplier.
- **External system queries**: Distance to nearest enemy, number of allies in range, inventory item counts.
- **Derived calculations**: Combined values from multiple sources that don't map to a single attribute or tag.
- **Platform data**: Input device state, network latency, frame-rate-dependent values.

The advantage over reading data inside a custom node is **reusability**: a resolver can be bound to any node's input property, combined with `ComparisonResolver` for branching, or used across multiple graphs without duplicating logic.

## Implementing IPropertyResolver

Implement `IPropertyResolver` to create a resolver that returns a single `Variant128` value:

```csharp
public interface IPropertyResolver
{
    Type ValueType { get; }
    Variant128 Resolve(GraphContext graphContext);
}
```

- **`ValueType`**: The type this resolver produces (e.g., `typeof(float)`, `typeof(bool)`). Used at graph construction time for validation when binding properties to node inputs.
- **`Resolve`**: Called at runtime each time a node reads the bound property. Receives the current `GraphContext` for accessing entity data, variables, and activation context.

### Example: Constant Resolver

The simplest resolver holds a fixed value. This is essentially what `VariantResolver` does:

```csharp
public class DifficultyMultiplierResolver : IPropertyResolver
{
    private readonly float _multiplier;

    public DifficultyMultiplierResolver(float multiplier)
    {
        _multiplier = multiplier;
    }

    public Type ValueType => typeof(float);

    public Variant128 Resolve(GraphContext graphContext)
    {
        return new Variant128(_multiplier);
    }
}
```

### Example: Reading from the Activation Context

Resolvers that need entity data should access it through the `GraphContext.ActivationContext`. When a graph is driven by an ability, this is an `AbilityBehaviorContext` containing the owner entity, source entity, and magnitude:

```csharp
public class EntityHealthPercentResolver : IPropertyResolver
{
    private readonly string _healthAttribute;
    private readonly string _maxHealthAttribute;

    public EntityHealthPercentResolver(string healthAttribute, string maxHealthAttribute)
    {
        _healthAttribute = healthAttribute;
        _maxHealthAttribute = maxHealthAttribute;
    }

    public Type ValueType => typeof(float);

    public Variant128 Resolve(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return default;
        }

        if (!context.Owner.Attributes.ContainsAttribute(_healthAttribute)
            || !context.Owner.Attributes.ContainsAttribute(_maxHealthAttribute))
        {
            return default;
        }

        int health = context.Owner.Attributes[_healthAttribute].CurrentValue;
        int maxHealth = context.Owner.Attributes[_maxHealthAttribute].CurrentValue;

        if (maxHealth == 0)
        {
            return default;
        }

        return new Variant128((float)health / maxHealth);
    }
}
```

Usage:

```csharp
graph.VariableDefinitions.DefineProperty("healthPercent",
    new EntityHealthPercentResolver(
        "CombatAttributeSet.Health",
        "CombatAttributeSet.MaxHealth"));

// Bind to a node input
timerNode.BindInput(TimerNode.DurationInput, "healthPercent");

// Or use in a comparison for branching
graph.VariableDefinitions.DefineProperty("isLowHealth",
    new ComparisonResolver(
        new EntityHealthPercentResolver(
            "CombatAttributeSet.Health",
            "CombatAttributeSet.MaxHealth"),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(0.25f), typeof(float))));
```

### Example: Reading from Graph Variables

Resolvers can also read from the graph's own mutable variables. This is useful for derived calculations that combine multiple graph values:

```csharp
public class ScaledDamageResolver : IPropertyResolver
{
    private readonly StringKey _baseDamageVariable;
    private readonly StringKey _multiplierVariable;

    public ScaledDamageResolver(string baseDamageVariable, string multiplierVariable)
    {
        _baseDamageVariable = baseDamageVariable;
        _multiplierVariable = multiplierVariable;
    }

    public Type ValueType => typeof(float);

    public Variant128 Resolve(GraphContext graphContext)
    {
        graphContext.GraphVariables.TryGetVar(_baseDamageVariable, out float baseDamage);
        graphContext.GraphVariables.TryGetVar(_multiplierVariable, out float multiplier);

        return new Variant128(baseDamage * multiplier);
    }
}
```

### Fallback Pattern

Follow the same defensive pattern used by the built-in resolvers: return a sensible default when data is unavailable. This keeps graphs functional even when contexts or data are missing.

```csharp
public Variant128 Resolve(GraphContext graphContext)
{
    // Return a default when the activation context isn't available
    if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
    {
        return default; // zero value
    }

    // Return a default when the expected data isn't present
    if (!context.Owner.Attributes.ContainsAttribute(_attributeKey))
    {
        return default;
    }

    // Happy path
    return new Variant128(context.Owner.Attributes[_attributeKey].CurrentValue);
}
```

## Implementing IArrayPropertyResolver

For resolvers that produce multiple values, implement `IArrayPropertyResolver`:

```csharp
public interface IArrayPropertyResolver
{
    Type ElementType { get; }
    Variant128[] ResolveArray(GraphContext graphContext);
}
```

- **`ElementType`**: The type of each element in the array (e.g., `typeof(int)`). Used for validation.
- **`ResolveArray`**: Returns an array of `Variant128` values.

Nodes read array properties through `GraphContext.TryResolveArray()`.

### Example: Array Resolver

```csharp
public class NearbyAllyCountResolver : IArrayPropertyResolver
{
    private readonly float _range;

    public NearbyAllyCountResolver(float range)
    {
        _range = range;
    }

    public Type ElementType => typeof(int);

    public Variant128[] ResolveArray(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return [];
        }

        // Query your game's spatial system for nearby allies
        int[] allyIds = FindAlliesInRange(context.Owner, _range);

        var result = new Variant128[allyIds.Length];
        for (int i = 0; i < allyIds.Length; i++)
        {
            result[i] = new Variant128(allyIds[i]);
        }

        return result;
    }

    private static int[] FindAlliesInRange(IForgeEntity owner, float range)
    {
        // Your game-specific spatial query here
        return [];
    }
}
```

Usage:

```csharp
graph.VariableDefinitions.DefineArrayProperty("nearbyAllies",
    new NearbyAllyCountResolver(10.0f));
```

## Composing Resolvers

Custom resolvers compose with built-in resolvers. The most common pattern is using a custom resolver as an operand in a `ComparisonResolver` to create data-driven conditions:

```csharp
// Custom resolver provides the value
var healthPercent = new EntityHealthPercentResolver(
    "CombatAttributeSet.Health",
    "CombatAttributeSet.MaxHealth");

// ComparisonResolver creates a boolean condition from it
graph.VariableDefinitions.DefineProperty("isLowHealth",
    new ComparisonResolver(
        healthPercent,
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(0.25f), typeof(float))));

// ExpressionNode branches on the condition
var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "isLowHealth");
```

You can also nest custom resolvers within other custom resolvers, as long as each one implements `IPropertyResolver`.

## Registering Resolvers

Custom resolvers are registered at graph construction time through `GraphVariableDefinitions.DefineProperty` (or `DefineArrayProperty` for array resolvers):

```csharp
var graph = new Graph();

// Single-value property
graph.VariableDefinitions.DefineProperty("timeOfDay",
    new TimeOfDayResolver());

// Array property
graph.VariableDefinitions.DefineArrayProperty("nearbyEnemies",
    new NearbyEnemyResolver(15.0f));
```

Once registered, any node can bind to the property by name:

```csharp
var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "timeOfDay");
```

### Type Validation

Use `ValidatePropertyType` at graph construction time to verify that a resolver's output type matches a node's expected input type:

```csharp
bool valid = graph.VariableDefinitions.ValidatePropertyType("timeOfDay", typeof(float));
// Returns true if the resolver's ValueType is assignable to float
```

## Resolution Order

When a node reads a named value through `GraphContext.TryResolve<T>()`:

1. **Graph variables** are checked first (mutable, per-execution state).
2. **Property definitions** (resolvers) are checked as a fallback (read-only, computed values).

This means a graph variable with the same name as a property definition will shadow the resolver. This can be useful for overriding a computed value with a fixed one during specific graph executions.
