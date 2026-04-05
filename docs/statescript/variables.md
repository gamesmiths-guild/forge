# Variables and Data

Statescript nodes communicate through **variables** and **property resolvers**. Variables hold mutable state during graph execution, while property resolvers provide read-only access to external data sources like entity attributes, tags, and constants.

For an overview of the Statescript system, see the [Statescript overview](README.md).

## Graph Variables

Graph variables are **mutable values** scoped to a single graph execution instance. They are defined at graph construction time with a name, type, and initial value through `GraphVariableDefinitions`.

```csharp
var graph = new Graph();

// Define a simple variable
graph.VariableDefinitions.DefineVariable("duration", 2.0);
graph.VariableDefinitions.DefineVariable("counter", 0);
graph.VariableDefinitions.DefineVariable("isActive", false);

// Define an array variable
graph.VariableDefinitions.DefineArrayVariable("targets", 0, 0, 0);
```

When the graph starts, each variable definition is copied into the `GraphContext.GraphVariables` instance, giving each execution independent mutable state.

**Supported types:** All types supported by `Variant128`: `bool`, `byte`, `sbyte`, `char`, `decimal`, `double`, `float`, `int`, `uint`, `long`, `ulong`, `short`, `ushort`, `Vector2`, `Vector3`, `Vector4`, `Plane`, `Quaternion`.

**Array variables** are also supported, holding a list of `Variant128` values.

### Reading and Writing Variables

Nodes interact with variables through two mechanisms:

- **Input Properties**: Declare that a node reads a named value at runtime. The name is bound via `Node.BindInput()`, and the value is resolved through the `GraphContext.TryResolve<T>()` method, which checks graph variables first, then falls back to property definitions.
- **Output Variables**: Declare that a node writes a named variable at runtime. The name and scope are bound via `Node.BindOutput()`.

```csharp
// Bind a timer node's duration input to the "duration" variable
var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "duration");

// Bind a SetVariableNode's input and output
var setVar = new SetVariableNode();
setVar.BindInput(SetVariableNode.SourceInput, "sourceProperty");
setVar.BindOutput(SetVariableNode.TargetOutput, "targetVariable");
```

### Variable Overrides at Start

When starting a graph, you can override variable values before the Entry node fires:

```csharp
processor.StartGraph(variables =>
{
    variables.SetVar("duration", 5.0);  // Override the initial value
    variables.SetVar("counter", 10);
});
```

This is used by `GraphAbilityBehavior<TData>` to inject typed activation data into graph variables.

## Shared Variables

Shared variables are **entity-level values** accessible by all Statescript graph instances running on the same entity. They live on the `IForgeEntity.SharedVariables` property and provide cross-ability communication.

> **Note:** [Attributes](../attributes.md) and [Tags](../tags.md) are also shared across all abilities on an entity and are often the preferred way to communicate state between graphs. Attributes handle numeric values and Tags handle boolean-like flags, and both integrate directly with the rest of Forge (effects, requirements, resolvers). Shared variables are useful when Attributes and Tags are not sufficient, for example when you need to share types beyond integer values and flags, or when you need entity-wide mutable state that doesn't map naturally to an attribute or tag.

```csharp
// Define shared variables on the entity
entity.SharedVariables.DefineVariable("comboCounter", 0);
entity.SharedVariables.DefineVariable("abilityLock", false);
```

When a graph runs as an ability behavior, `GraphAbilityBehavior` automatically sets `GraphContext.SharedVariables` to the owner entity's `SharedVariables`. Nodes and resolvers can then read and write shared state.

**Example use cases:**

- A "combo counter" incremented by an attack ability and read by a finisher ability.
- An "ability lock" flag preventing multiple abilities from executing simultaneously.
- A "last target" reference shared between targeting and execution phases.

### Writing to Shared Variables

The `SetVariableNode` can write to shared variables by setting its output scope:

```csharp
var setSharedVar = new SetVariableNode();
setSharedVar.BindInput(SetVariableNode.SourceInput, "localValue");
setSharedVar.BindOutput(SetVariableNode.TargetOutput, "comboCounter", VariableScope.Shared);
```

### Shared vs. Graph Variables

| | Graph Variables | Shared Variables |
|---|---|---|
| **Scope** | Single graph execution | Entity-wide |
| **Lifetime** | Created when graph starts, destroyed when graph ends | Persists for entity lifetime |
| **Visibility** | Only the current graph instance | All graph instances on the same entity |
| **Definition** | `GraphVariableDefinitions.DefineVariable` | `entity.SharedVariables.DefineVariable` |

## Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Each resolver implements `IPropertyResolver` and returns a `Variant128` given a `GraphContext`.

Properties are defined at graph construction time through `GraphVariableDefinitions.DefineProperty`:

```csharp
graph.VariableDefinitions.DefineProperty("ownerHealth",
    new AttributeResolver("CombatAttributeSet.Health"));

graph.VariableDefinitions.DefineProperty("isEnraged",
    new TagResolver(Tag.RequestTag(tagsManager, "status.enraged")));

graph.VariableDefinitions.DefineProperty("healthBelowHalf",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.LessThan,
        new VariantResolver(new Variant128(50), typeof(int))));
```

### Resolution Order

When a node reads a named value through `GraphContext.TryResolve<T>()`:

1. **Graph variables** are checked first (mutable, per-execution state).
2. **Property definitions** are checked as a fallback (read-only, computed values).

This means a graph variable can "shadow" a property definition with the same name.

### Built-in Resolvers

#### VariableResolver

Reads a graph variable by name. Useful for property expressions that reference mutable graph variables.

```csharp
new VariableResolver("counter", typeof(int))
```

#### SharedVariableResolver

Reads a shared variable from the entity's shared variable bag. Enables cross-ability communication.

```csharp
new SharedVariableResolver("comboCounter", typeof(int))
```

#### VariantResolver

Holds a fixed constant value. Useful as the right-hand side of a `ComparisonResolver`.

```csharp
new VariantResolver(new Variant128(50), typeof(int))    // Constant 50
new VariantResolver(new Variant128(3.14f), typeof(float)) // Constant 3.14
```

#### AttributeResolver

Reads the `CurrentValue` of a specific entity attribute. Requires the graph to be driven by an ability (accesses the owner entity from `AbilityBehaviorContext` stored in `GraphContext.ActivationContext`).

```csharp
new AttributeResolver("CombatAttributeSet.Health")
```

Returns `int` (the attribute's `CurrentValue`). Returns `0` if no activation context is available or the attribute doesn't exist.

#### TagResolver

Checks whether the owner entity has a specific gameplay tag. Requires ability activation context.

```csharp
new TagResolver(Tag.RequestTag(tagsManager, "status.enraged"))
```

Returns `bool`. Returns `false` if no activation context is available.

#### ComparisonResolver

Compares two values using a `ComparisonOperation` and returns a `bool`. Both operands are resolved through their own `IPropertyResolver` instances and converted to `double` for comparison.

```csharp
// "Is health greater than 50?"
new ComparisonResolver(
    new AttributeResolver("CombatAttributeSet.Health"),
    ComparisonOperation.GreaterThan,
    new VariantResolver(new Variant128(50), typeof(int)))
```

**Supported operations:** `Equal`, `NotEqual`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`.

Supports nesting: operands can be other `ComparisonResolver` instances or any `IPropertyResolver`, enabling arbitrarily complex expressions. Use as the condition input for an `ExpressionNode` to create data-driven branching without custom C# code.

```csharp
// "Is health > 50 AND is enraged?"
// Use two separate properties bound to an ExpressionNode, or nest comparisons:
graph.VariableDefinitions.DefineProperty("healthAbove50",
    new ComparisonResolver(
        new AttributeResolver("CombatAttributeSet.Health"),
        ComparisonOperation.GreaterThan,
        new VariantResolver(new Variant128(50), typeof(int))));
```

#### MagnitudeResolver

Reads the `Magnitude` float from the ability's activation context. This is the numeric value passed during `AbilityHandle.Activate()` or propagated from an event trigger's `EventMagnitude`.

```csharp
new MagnitudeResolver()
```

Returns `float`. Returns `0` if no activation context is available.

### Array Property Resolvers

For properties that resolve to arrays, implement `IArrayPropertyResolver`:

```csharp
graph.VariableDefinitions.DefineArrayProperty("nearbyEnemies", myCustomArrayResolver);
```

Nodes can read array properties through `GraphContext.TryResolveArray()`.

### Creating Custom Resolvers

Implement `IPropertyResolver` (or `IArrayPropertyResolver` for arrays) to create custom data sources that expose game-specific values to graph nodes. For a full guide with examples covering activation context access, composition with built-in resolvers, and best practices, see [Custom Resolvers](resolvers.md).

## Input Properties and Output Variables

### InputProperty

An `InputProperty` declares that a node reads a named value. It has a `Label` (for editor display), an `ExpectedType` (for validation), and a `BoundName` (set via `Node.BindInput`).

### OutputVariable

An `OutputVariable` declares that a node writes a named variable. It has a `Label`, a `ValueType`, a `Scope` (`Graph` or `Shared`), and a `BoundName` (set via `Node.BindOutput`).

### Type Validation

Use `GraphVariableDefinitions.ValidatePropertyType()` at graph construction time to verify that a bound property produces a value compatible with a node's expected input type:

```csharp
bool valid = graph.VariableDefinitions.ValidatePropertyType("duration", typeof(double));
```

## Data Flow Summary

```
+----------------------------------------------------------+
|                    GraphContext                          |
|                                                          |
|  +-------------+  +-------------+  +----------------+    |
|  | Graph Vars  |  | Shared Vars |  | Activation Ctx |    |
|  | (per-graph) |  | (per-entity)|  | (ability data) |    |
|  +------+------+  +------+------+  +-------+--------+    |
|         |                |                 |             |
|         +--------+-------+-----------------+             |
|                  |                                       |
|          +-------v-------+                               |
|          |   Resolvers   |  Attribute, Tag, Comparison,  |
|          |               |  Variable, Shared, Magnitude, |
|          |               |  Variant                      |
|          +-------+-------+                               |
|                  |                                       |
|          +-------v-------+                               |
|          |  Node Inputs  |                               |
|          +---------------+                               |
+----------------------------------------------------------+
```
