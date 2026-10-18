# Statescript in Code

Building a Statescript graph with the C# API and running it as an ability. Every guide here builds on the same small setup: a `PlayerAttributeSet`, a `Player` entity, and a `TagsManager` and `CuesManager`. It is defined once in [Guides](README.md#the-shared-setup); the code samples assume it.

An `IAbilityBehavior` written by hand, like the ones in [Ability Recipes](ability-recipes.md), is the imperative way to give an ability its logic. [Statescript](../statescript/README.md) is the declarative one: a graph of nodes describing what happens, in what order, and for how long.

Statescript is designed as a **visual** scripting language, but the core library has nothing visual in it — by design. The core owns the graph model and the processor that executes it; the graph *editors* live in the engine integrations, each rendering the same model in its own editor UI. This section shows the C# API those editors ultimately write to, which is also how you build a graph without any editor at all.

## Building a Graph

A graph is nodes plus the connections between them. This one waits one second, then applies an effect to the ability's target. `damageEffectData` is any `EffectData`, such as an instant -10 Health:

```csharp
var graph = new Graph();

// Inputs the nodes read from. Variables hold values; properties are computed on read.
graph.VariableDefinitions.DefineVariable("delay", 1.0);
graph.VariableDefinitions.DefineObjectProperty("damageEffect", new EffectFromDataResolver(damageEffectData));
graph.VariableDefinitions.DefineObjectProperty("target", new AbilityTargetResolver());

// A state node: stays active for its bound duration, then emits OnTimerEnd.
var timer = new TimerNode();
timer.BindInput(TimerNode.DurationInput, "delay");
graph.AddNode(timer);

// An action node: fires once and continues.
var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "damageEffect");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "target");
graph.AddNode(applyEffect);

// Entry -> Timer, and Timer's OnTimerEnd -> ApplyEffect.
graph.AddConnection(new Connection(
    graph.EntryNode.OutputPorts[EntryNode.OutputPort],
    timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

graph.AddConnection(new Connection(
    timer.OutputPorts[TimerNode.OnTimerEndPort],
    applyEffect.InputPorts[ActionNode.InputPort]));
```

Three things to notice, because they are the whole model:

- **Nodes read their inputs through the variable definitions**, never from each other. `BindInput` names a variable or property; `AbilityTargetResolver` pulls the target out of the ability that is running the graph, so the same graph works for whoever activates it.
- **Ports are numbered and typed.** A state node like `TimerNode` has several output ports — `OnActivate`, `OnDeactivate`, `OnAbort`, `Subgraph`, `OnTimerEnd` — and connecting to the right one is what expresses "only when the timer finished on its own".
- **State nodes own time; action nodes do not.** The timer stays active across frames, the effect application happens and is done.

## Running It as an Ability

`GraphAbilityBehavior` implements `IAbilityBehavior`, so a graph plugs into an ability exactly like a hand-written behavior. `enemy` stands for any other `IForgeEntity`:

```csharp
var abilityData = new AbilityData(
    "Delayed Strike",
    behaviorFactory: () => new GraphAbilityBehavior(graph));

AbilityHandle handle = player.Abilities.GrantAbilityPermanently(
    abilityData, 1, LevelComparison.None, player);

// The graph applies the effect to whoever the ability was activated on.
handle.TryActivate(out AbilityActivationFailures failureFlags, enemy);
```

Activating the ability starts the graph at its Entry node; `UpdateAbilities` ticks it each frame, and the ability instance ends automatically when the graph finishes. Everything you already know about costs, cooldowns, tag requirements and triggers applies unchanged — the graph replaces the behavior, not the ability.

From here the [Statescript overview](../statescript/README.md) covers the [node catalog](../statescript/nodes/README.md), the [resolvers](../statescript/resolvers/README.md) that feed node inputs, [variables and data flow](../statescript/variables.md), and [ability integration](../statescript/ability-integration.md) in depth.

