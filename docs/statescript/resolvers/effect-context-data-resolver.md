# EffectContextDataResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectContextDataResolver`
> **Output Type:** `EffectApplicationContext`

Produces the custom application context data passed when a graph applies an effect. It delegates to an `IEffectContextDataProvider`, which builds a strongly-typed value from the current graph state and wraps it in an `EffectApplicationContext`. Bind it to the optional **Context Data** input of [ApplyEffectNode](../nodes/action/apply-effect-node.md) and [EffectNode](../nodes/state/effect-node.md).

This is the inverse of [AbilityActivationDataResolver](ability-activation-data-resolver.md): instead of reading values *out* of typed data the ability system supplied, a provider builds typed data *from* the graph and feeds it *into* the effect pipeline, where custom calculators and executions read it through `EffectEvaluatedData.TryGetContextData<TData>`.

## Constructor

```csharp
new EffectContextDataResolver(provider)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| provider | `IEffectContextDataProvider` | The provider that builds the application context from the graph state. |

## Defining a provider

Derive from `EffectContextDataProvider<TData>` and override `CreateData`. The base class wraps the returned value in an `EffectApplicationContext<TData>` automatically.

```csharp
public sealed record DamageContext(float Damage, bool IsCritical);

public sealed class DamageContextProvider : EffectContextDataProvider<DamageContext>
{
    public override DamageContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
    {
        graphContext.TryResolve("damage", out float damage);
        graphContext.TryResolve("isCritical", out bool isCritical);
        return new DamageContext(damage, isCritical);
    }
}
```

The same `TData` is then available wherever the effect is evaluated:

```csharp
public override float CalculateBaseMagnitude(
    Effect effect,
    IForgeEntity target,
    EffectEvaluatedData? effectEvaluatedData)
{
    if (effectEvaluatedData?.TryGetContextData(out DamageContext? context) == true)
    {
        return context.IsCritical ? context.Damage * 2 : context.Damage;
    }

    return 0f;
}
```

## Declaring authored inputs

Instead of reading values from named graph variables, a provider can declare **inputs** that the graph editor renders as nested resolvers directly on the node's Context Data section. Override `Inputs` and read the resolved values from the `EffectContextDataInputs` bag:

```csharp
public sealed record DirectionContext(System.Numerics.Vector3 Direction);

public sealed class DirectionContextProvider : EffectContextDataProvider<DirectionContext>
{
    public override IReadOnlyList<EffectContextDataInput> Inputs =>
        [new EffectContextDataInput("Direction", typeof(System.Numerics.Vector3))];

    public override DirectionContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
    {
        return new DirectionContext(inputs.Get<System.Numerics.Vector3>("Direction"));
    }
}
```

Each declared input renders its own resolver dropdown (constant, variable, activation data, math, …) in the editor, so a designer can author the value without touching graph variables. `EffectContextDataInputs.Get<T>` reads the resolved value (`default` when no resolver is bound); input value types must be supported by `Variant128`. The same `Name` is used both as the editor label and as the key passed to `Get<T>`.

## Behavior

- On each resolve, calls `provider.CreateContext(graphContext, inputs)`, which boxes the provider's `TData` into an `EffectApplicationContext<TData>`. Declared inputs are resolved lazily from the bag as the provider reads them.
- The node resolves the context once per execution and passes the same instance to every application in the `effect[] x target[]` cross-product.
- When the **Context Data** input is unbound, the node applies effects without context data.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "damageContext",
    new EffectContextDataResolver(new DamageContextProvider()));

var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "burnEffect");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "target");
applyEffect.BindInput(ApplyEffectNode.ContextDataInput, "damageContext");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
- [AbilityActivationDataResolver](ability-activation-data-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
