# CueCustomParametersResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.CueCustomParametersResolver`
> **Output Type:** `Dictionary<StringKey, object>`

Produces the custom parameter bag attached to a cue's `CueParameters.CustomParameters` when a graph fires a cue. It delegates to an `ICueCustomParametersProvider`, which builds a dictionary keyed by `StringKey` from the current graph state. Bind it to the optional **Custom Parameters** input of [ExecuteCueNode](../nodes/action/execute-cue-node.md), [UpdateCueNode](../nodes/action/update-cue-node.md), and [CueNode](../nodes/state/cue-node.md).

A cue handler then reads the values back by key from the `CueParameters` it receives. This is the cue-side analog of [EffectContextDataResolver](effect-context-data-resolver.md): instead of producing an `EffectApplicationContext` for the effect pipeline, it produces the parameter dictionary the cue handler consumes.

## Constructor

```csharp
new CueCustomParametersResolver(provider)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| provider | `ICueCustomParametersProvider` | The provider that builds the custom parameter bag from the graph state. |

## Defining a provider

Derive from `CueCustomParametersProvider` and override `CreateCustomParameters`. Return a `Dictionary<StringKey, object>` whose keys are whatever your cue handler reads back.

```csharp
public sealed class DamageCueParametersProvider : CueCustomParametersProvider
{
    public override Dictionary<StringKey, object> CreateCustomParameters(
        GraphContext graphContext,
        CueCustomParameterInputs inputs)
    {
        graphContext.TryResolve("damage", out int damage);
        graphContext.TryResolve("isCritical", out bool isCritical);

        return new Dictionary<StringKey, object>
        {
            ["damage"] = damage,
            ["isCritical"] = isCritical,
        };
    }
}
```

The same keys are then available in the cue handler:

```csharp
public void OnExecute(IForgeEntity? target, CueParameters? parameters)
{
    if (parameters?.CustomParameters is { } custom && custom.TryGetValue("damage", out object? damage))
    {
        var amount = (int)damage;
        // ...
    }
}
```

## Declaring authored inputs

Instead of reading values from named graph variables, a provider can declare **inputs** that the graph editor renders as nested resolvers directly on the node's Custom Parameters section. Override `Inputs` and read the resolved values from the `CueCustomParameterInputs` bag:

```csharp
public sealed class StrengthCueParametersProvider : CueCustomParametersProvider
{
    public override IReadOnlyList<CueCustomParameterInput> Inputs =>
        [new CueCustomParameterInput("Strength", typeof(int))];

    public override Dictionary<StringKey, object> CreateCustomParameters(
        GraphContext graphContext,
        CueCustomParameterInputs inputs)
    {
        return new Dictionary<StringKey, object> { ["strength"] = inputs.Get<int>("Strength") };
    }
}
```

Each declared input renders its own resolver dropdown (constant, variable, activation data, math, ...) in the editor, so a designer can author the value without touching graph variables. `CueCustomParameterInputs.Get<T>` reads the resolved value (`default` when no resolver is bound); declared input value types must be supported by `Variant128`. The same `Name` is used both as the editor label and as the key passed to `Get<T>`. A provider that needs object-lane values (entities, tags) is not limited by this: it can read them directly from `graphContext` and box them into the bag.

## Behavior

- On each resolve, calls `provider.CreateCustomParameters(graphContext, inputs)`, which builds a fresh dictionary. Declared inputs are resolved lazily from the bag as the provider reads them.
- The node resolves the bag once per execution and shares the same dictionary across the whole `cueTag[] x target[]` matrix, attaching it to the `CueParameters.CustomParameters` of every fired cue.
- When the **Custom Parameters** input is unbound, cues fire without custom parameters (the dictionary is `null`).

## Usage

```csharp
graph.VariableDefinitions.DefineObjectProperty(
    "damageParams",
    new CueCustomParametersResolver(new DamageCueParametersProvider()));

var executeCue = new ExecuteCueNode();
executeCue.BindInput(ExecuteCueNode.CueTagInput, "hitCue");
executeCue.BindInput(ExecuteCueNode.TargetInput, "target");
executeCue.BindInput(ExecuteCueNode.CustomParametersInput, "damageParams");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectContextDataResolver](effect-context-data-resolver.md)
- [ExecuteCueNode](../nodes/action/execute-cue-node.md)
- [UpdateCueNode](../nodes/action/update-cue-node.md)
- [CueNode](../nodes/state/cue-node.md)
