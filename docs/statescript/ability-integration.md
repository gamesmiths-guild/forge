# Ability Integration

Statescript graphs integrate with the [Abilities system](../abilities.md) through `GraphAbilityBehavior`, which implements `IAbilityBehavior` and drives an ability's lifecycle through a graph. This page covers how to connect graphs to abilities, handle activation data, and manage the update loop.

For an overview of the Statescript system, see the [Statescript overview](README.md).

## GraphAbilityBehavior

`GraphAbilityBehavior` bridges the ability lifecycle and the graph processor:

- When the ability **starts** → the graph begins processing from its Entry node.
- Each frame → `OnUpdate(deltaTime)` advances all active state nodes.
- When the graph **completes** (all state nodes deactivate) or an Exit node is reached → the ability instance ends automatically.
- When the ability is **canceled** → the graph is stopped and all active nodes are disabled.

```csharp
var graph = new Graph();
graph.VariableDefinitions.DefineVariable("duration", 2.0);

var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "duration");
graph.AddNode(timer);
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

// Create the behavior
var behavior = new GraphAbilityBehavior(graph);

// Use it in an ability
var abilityData = new AbilityData(
    "Channeled Spell",
    behaviorFactory: () => behavior);
```

### How It Works Internally

When the ability activates:

1. `OnStarted(context)` is called by the Abilities system.
2. `GraphAbilityBehavior` sets `GraphContext.SharedVariables` to the owner entity's `SharedVariables`.
3. `GraphContext.ActivationContext` is set to the `AbilityBehaviorContext`.
4. `GraphProcessor.OnGraphCompleted` is wired to `context.InstanceHandle.End()`.
5. `GraphProcessor.StartGraph()` is called, beginning execution.

When the ability is canceled:

1. `OnEnded(context)` is called by the Abilities system.
2. The `OnGraphCompleted` callback is cleared (to prevent re-entrant calls).
3. `GraphProcessor.StopGraph()` is called if the graph is still running.

### The Update Loop

`GraphAbilityBehavior` implements `IAbilityBehavior.OnUpdate(deltaTime)`, which calls `GraphProcessor.UpdateGraph(deltaTime)` to tick all active state nodes. The Abilities system calls this automatically each frame for active ability instances.

```csharp
// In your game loop, update the entity's abilities
// (This is handled by whatever drives your abilities, typically alongside EffectsManager)
behavior.Processor.UpdateGraph(deltaTime);
```

If you're using `GraphAbilityBehavior` through the standard Abilities system, the update is called automatically by the ability instance.

## Typed Activation Data

For abilities that receive strongly-typed activation data, use `GraphAbilityBehavior<TData>`.

When your graph only needs to read supported scalar or math types directly from the activation payload, bind them through `ActivationDataResolver`:

```csharp
public record struct DashData(float Distance, float Speed);

var graph = new Graph();

graph.VariableDefinitions.DefineProperty("distance",
    new ActivationDataResolver(typeof(DashData), nameof(DashData.Distance)));

graph.VariableDefinitions.DefineProperty("speed",
    new ActivationDataResolver(typeof(DashData), nameof(DashData.Speed)));

// ... build graph nodes ...

var behavior = new GraphAbilityBehavior<DashData>(graph);

var abilityData = new AbilityData(
    "Dash",
    behaviorFactory: () => behavior);
```

When activated with typed data:

```csharp
handle.Activate(new DashData(10.0f, 5.0f), out AbilityActivationFailures failures);
```

If you need to rename fields, precompute values, or convert unsupported payload types into graph-friendly values, use the data-binder overload instead:

```csharp
var behavior = new GraphAbilityBehavior<DashData>(graph, (data, variables) =>
{
    variables.SetVar("distance", data.Distance);
    variables.SetVar("speed", data.Speed);
});
```

The data binder runs after variables are initialized from definitions but before the Entry node fires, ensuring nodes can read the activation data from their first message.

## Activation Context

When a graph is driven by an ability, the `AbilityBehaviorContext` is stored in `GraphContext.ActivationContext`. Nodes can access it to interact with the ability system:

```csharp
public class CommitAbilityActionNode : ActionNode
{
    protected override void Execute(GraphContext graphContext)
    {
        if (graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            context.AbilityHandle.CommitAbility();
        }
    }
}
```

### Available Context Data

Through `AbilityBehaviorContext`, nodes have access to:

| Property | Type | Description |
|----------|------|-------------|
| **Owner** | `IForgeEntity` | The entity that owns the ability. |
| **Source** | `IForgeEntity?` | The entity that granted the ability. |
| **Target** | `IForgeEntity?` | The target passed during activation. |
| **Level** | `int` | The ability's current level. |
| **AbilityHandle** | `AbilityHandle` | Handle for committing cost/cooldown. |
| **InstanceHandle** | `AbilityInstanceHandle` | Handle for ending the ability instance. |
| **Magnitude** | `float` | Numeric value from the activation attempt. |

### Property Resolvers and Activation Context

Several built-in [property resolvers](variables.md#built-in-resolvers) read from the activation context:

- **`AttributeResolver`**: Reads an attribute from the owner entity.
- **`TagQueryResolver`**: Evaluates a tag query against the owner entity's tags.
- **`MagnitudeResolver`**: Reads the activation magnitude.

These resolvers gracefully return default values when the graph runs without an ability context (e.g., standalone graph execution).

## Standalone Graph Execution

Graphs can also run independently of the Abilities system using `GraphProcessor` directly:

```csharp
var graph = new Graph();
// ... build graph ...

var processor = new GraphProcessor(graph);
processor.OnGraphCompleted = () => Console.WriteLine("Graph finished!");
processor.StartGraph();

// Drive the graph in your game loop
void Update(float deltaTime)
{
    processor.UpdateGraph(deltaTime);
}
```

In standalone mode:

- `GraphContext.ActivationContext` is `null`.
- `GraphContext.SharedVariables` is `null` (unless manually set).
- Resolvers that depend on `AbilityBehaviorContext` return default values.

You can still set shared variables and activation context manually:

```csharp
var processor = new GraphProcessor(graph, sharedVariables: mySharedVars);
processor.GraphContext.ActivationContext = myCustomContext;
```

## Flyweight Pattern

A `Graph` instance is an immutable definition. Multiple `GraphProcessor` instances can share the same `Graph`, each with independent runtime state through their own `GraphContext`:

```csharp
var graph = new Graph();
// ... build graph once ...

// Two independent executions of the same graph
var processor1 = new GraphProcessor(graph);
var processor2 = new GraphProcessor(graph);

processor1.StartGraph();
processor2.StartGraph();

// Each has independent variable state
processor1.GraphContext.GraphVariables  // Independent
processor2.GraphContext.GraphVariables  // Independent
```

This is especially useful for `PerExecution` instancing policies where the same ability definition can have multiple concurrent activations.

## Common Patterns

### Commit Cost and Cooldown from Graph

```csharp
public class CommitAbilityActionNode : ActionNode
{
    protected override void Execute(GraphContext graphContext)
    {
        if (graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            context.AbilityHandle.CommitAbility();
        }
    }
}
```

Wire this node after the Entry to commit the ability's cost and cooldown at the start of execution.

### Apply Effects from Graph

```csharp
public class ApplyEffectToOwnerNode : ActionNode
{
    private readonly EffectData _effectData;

    public ApplyEffectToOwnerNode(EffectData effectData)
    {
        _effectData = effectData;
    }

    protected override void Execute(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return;
        }

        var effect = new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));
        context.Owner.EffectsManager.ApplyEffect(effect);
    }
}
```

### Conditional Logic Based on Entity State

Use property resolvers with `ExpressionNode` to branch based on runtime state:

```csharp
graph.VariableDefinitions.DefineProperty("isEnraged",
    new TagQueryResolver(Tag.RequestTag(tagsManager, "status.enraged")));

var expression = new ExpressionNode();
expression.BindInput(ExpressionNode.ConditionInput, "isEnraged");
graph.AddNode(expression);

// True branch: enhanced ability
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.TruePort],
    enhancedEffect.InputPorts[ActionNode.InputPort]));

// False branch: normal ability
graph.AddConnection(new Connection(
    expression.OutputPorts[ConditionNode.FalsePort],
    normalEffect.InputPorts[ActionNode.InputPort]));
```

### Timed Ability with Graph

A complete example of an ability that applies a buff, waits, then cleans up:

```csharp
// Build the graph
var graph = new Graph();
graph.VariableDefinitions.DefineVariable("buffDuration", 5.0);

var commitNode = new CommitAbilityActionNode();
var applyBuff = new ApplyEffectToOwnerNode(buffEffectData);
var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "buffDuration");
var removeBuff = new RemoveEffectActionNode(); // Custom node

graph.AddNode(commitNode);
graph.AddNode(applyBuff);
graph.AddNode(timer);
graph.AddNode(removeBuff);

// Entry → commit → apply buff → timer
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    commitNode.InputPorts[ActionNode.InputPort]));
graph.AddConnection(new Connection(
    commitNode.OutputPorts[ActionNode.OutputPort],
    applyBuff.InputPorts[ActionNode.InputPort]));
graph.AddConnection(new Connection(
    applyBuff.OutputPorts[ActionNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

// Timer deactivates → remove buff
graph.AddConnection(new Connection(
    timer.OutputPorts[StateNode<TimerNodeContext>.OnDeactivatePort],
    removeBuff.InputPorts[ActionNode.InputPort]));

// Create the ability
var behavior = new GraphAbilityBehavior(graph);
var abilityData = new AbilityData(
    "Shield Buff",
    behaviorFactory: () => behavior);
```

## Best Practices

1. **Remember to Commit**: Usually you want to place a `CommitAbility` action node early in the graph to apply costs and cooldowns, but you may want to test conditions first. Make sure to commit at some point to activate cooldowns and deduct costs.
2. **Use Subgraphs for Cleanup**: Connect ongoing effects and state to Subgraph ports so they are automatically cleaned up when the parent deactivates or the ability is canceled.
3. **Leverage Property Resolvers**: Use `AttributeResolver`, `TagQueryResolver`, and `ComparisonResolver` for data-driven conditions instead of writing custom condition nodes.
4. **Use Shared Variables for Cross-Ability State**: When multiple abilities need to communicate and [Attributes](../attributes.md) or [Tags](../tags.md) are not sufficient, use entity-level shared variables rather than external state.
5. **Keep Graphs Focused**: Each graph should represent one ability behavior. Share logic through custom node classes rather than making graphs overly complex.
