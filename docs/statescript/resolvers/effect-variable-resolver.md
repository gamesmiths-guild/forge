# EffectVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectVariableResolver`
> **Output Type:** `Effect?`

Reads an `Effect` instance from graph or shared object-backed variables, so a single effect can be stored once and reused across multiple applications.

## Constructor

```csharp
new EffectVariableResolver(variableName)
new EffectVariableResolver(variableName, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the object-backed variable to read. |
| scope | `VariableScope` | Chooses whether to read from graph variables (default) or shared variables. |

## Behavior

- Reads an `Effect` reference from `Variables`.
- Supports both graph-local and shared variable scopes.
- Returns `null` if the variable does not exist or currently has no effect assigned.
- Returns the same instance that was stored, so mutating it (for example `Effect.LevelUp()`) affects any non-snapshot active applications of that effect live.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectVariable<Effect>("storedEffect");

// Store an effect instance once...
graph.VariableDefinitions.DefineObjectProperty(
    "effectSource",
    new EffectFromDataResolver(burnEffectData));
var storeEffect = new SetVariableNode();
storeEffect.BindInput(SetVariableNode.SourceInput, "effectSource");
storeEffect.BindOutput(SetVariableNode.TargetOutput, "storedEffect");

// ...then reuse it on a node input.
graph.VariableDefinitions.DefineObjectProperty(
    "effectVar",
    new EffectVariableResolver("storedEffect"));
var applyEffect = new ApplyEffectNode();
applyEffect.BindInput(ApplyEffectNode.EffectInput, "effectVar");
applyEffect.BindInput(ApplyEffectNode.TargetInput, "target");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectArrayVariableResolver](effect-array-variable-resolver.md)
- [EffectFromDataResolver](effect-from-data-resolver.md)
- [ApplyEffectNode](../nodes/action/apply-effect-node.md)
- [EffectNode](../nodes/state/effect-node.md)
