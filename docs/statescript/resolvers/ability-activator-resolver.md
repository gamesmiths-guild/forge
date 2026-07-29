# AbilityActivatorResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.AbilityActivatorResolver`
> **Output Type:** `AbilityActivator`

Produces the custom activation data a graph passes when it activates an ability. It delegates to an `IAbilityActivationDataProvider`, which builds a strongly-typed value from the current graph state and hands it to the generic activation APIs. Bind it to the optional **Activation Data** input of [TryActivateAbilityNode](../nodes/condition/try-activate-ability-node.md), [TryActivateAbilitiesByTagNode](../nodes/condition/try-activate-abilities-by-tag-node.md), and [GrantAbilityAndActivateOnceNode](../nodes/condition/grant-ability-and-activate-once-node.md).

This is the send end of the channel [AbilityActivationDataResolver](ability-activation-data-resolver.md) reads from: a provider builds typed data *from* the graph and feeds it *into* the activation, where the activated ability reads members back *out*. **Both ends are driven by the same `IAbilityActivationDataProvider`**, so one implementation per activation-data type covers the whole round trip. It is the ability counterpart of [EffectContextDataResolver](effect-context-data-resolver.md) and [EventPayloadResolver](event-payload-resolver.md).

## Constructor

```csharp
new AbilityActivatorResolver(provider)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| provider | `IAbilityActivationDataProvider` | The provider that builds the activation data from the graph state. |

## Defining a provider

Derive from `AbilityActivationDataProvider<TData>` and override `CreateData`. The base class calls the generic activation API matching the node with the concrete `TData`, so the data reaches the ability without being boxed.

```csharp
public sealed record ThrowData(float Force, bool IsCharged);

public sealed class ThrowDataProvider : AbilityActivationDataProvider<ThrowData>
{
    public override ThrowData CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
    {
        graphContext.TryResolve("force", out float force);
        graphContext.TryResolve("isCharged", out bool isCharged);
        return new ThrowData(force, isCharged);
    }
}
```

The activated ability's behavior receives the same `TData`:

```csharp
public sealed class ThrowBehavior : IAbilityBehavior<ThrowData>
{
    public void OnStarted(AbilityBehaviorContext context, ThrowData data)
    {
        // data.Force / data.IsCharged
    }

    public void OnEnded(AbilityBehaviorContext context)
    {
    }
}
```

When the activated ability is itself driven by a graph, use `GraphAbilityBehavior<ThrowData>` and read the fields back with [AbilityActivationDataResolver](ability-activation-data-resolver.md) (or map them into graph variables with a data binder).

## Declaring members

Declare `Members` **once** and both directions use that one list — the sending node authors them, the receiving graph binds them:

```csharp
public sealed record AimData(System.Numerics.Vector3 Direction);

public sealed class AimDataProvider : AbilityActivationDataProvider<AimData>
{
    public override IReadOnlyList<AbilityActivationDataMember> Members =>
        [new AbilityActivationDataMember("Direction", typeof(System.Numerics.Vector3))];

    public override AimData CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
    {
        return new AimData(inputs.Get<System.Numerics.Vector3>("Direction"));
    }
}
```

On the **sending** side each member renders its own resolver dropdown (constant, variable, activation data, math, ...) on the node's Activation Data section, so a designer can author the value without touching graph variables. `AbilityActivationDataInputs.Get<T>` reads the resolved value (`default` when no resolver is bound).

On the **reading** side the same member is offered as a bindable field of [AbilityActivationDataResolver](ability-activation-data-resolver.md), which reads it off the activation data. So `Name` must match the public field or property on `TData` — the reading side resolves it there by name, and it cannot be an alias.

`ValueType` is the type **as the graph sees it**, so it must be supported by `Variant128` — declare `System.Numerics.Vector3` rather than an engine-specific vector even when `TData` stores the engine's. Converting between the two is `CreateData`'s job on the way in, and the engine integration's on the way out.

## Behavior

- The resolver returns the same `AbilityActivator` on every resolve; the *data* is rebuilt from the current graph state on each activation. Declared inputs are resolved lazily from the bag as the provider reads them.
- Each node calls the activator entry point matching its own operation, which forwards to the generic ability API (`AbilityHandle.Activate<TData>`, `EntityAbilities.TryActivateAbilitiesByTag<TData>`, `EntityAbilities.GrantAbilityAndActivateOnce<TData>`).
- **Mismatched data is never an error.** An ability whose behavior does not implement `IAbilityBehavior<TData>` still activates; it just starts through the untyped path and ignores the data. This matters most for [TryActivateAbilitiesByTagNode](../nodes/condition/try-activate-abilities-by-tag-node.md), where one tag can select several abilities that do not share an activation-data type.
- When the **Activation Data** input is unbound, the node activates without custom data.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "throwData",
    new AbilityActivatorResolver(new ThrowDataProvider()));

var tryActivate = new TryActivateAbilityNode();
tryActivate.BindInput(TryActivateAbilityNode.AbilityInput, "throwAbility");
tryActivate.BindInput(TryActivateAbilityNode.ActivationDataInput, "throwData");
```

## See Also

- [Resolvers Overview](README.md)
- [AbilityActivationDataResolver](ability-activation-data-resolver.md)
- [EffectContextDataResolver](effect-context-data-resolver.md)
- [EventPayloadResolver](event-payload-resolver.md)
- [TryActivateAbilityNode](../nodes/condition/try-activate-ability-node.md)
- [TryActivateAbilitiesByTagNode](../nodes/condition/try-activate-abilities-by-tag-node.md)
- [GrantAbilityAndActivateOnceNode](../nodes/condition/grant-ability-and-activate-once-node.md)
