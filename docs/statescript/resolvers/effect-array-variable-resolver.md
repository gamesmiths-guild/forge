# EffectArrayVariableResolver

> **Type:** `Gamesmiths.Forge.Statescript.Properties.EffectArrayVariableResolver`
> **Output Type:** `Effect[]`

Reads an array of `Effect` instances from graph or shared object-backed variables so a set of effects can be stored once and reused across applications.

## Constructor

```csharp
new EffectArrayVariableResolver(variableName)
new EffectArrayVariableResolver(variableName, scope)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| variableName | `StringKey` | The name of the object-backed array variable to read. |
| scope | `VariableScope` | Chooses whether to read from graph variables (default) or shared variables. |

## Behavior

- Reads an `Effect` reference array from `Variables`.
- Supports both graph-local and shared variable scopes.
- Returns an empty array if the variable does not exist.
- Returns the stored instances, so mutating any of them affects their non-snapshot active applications live.

## Usage

```csharp
graph.VariableDefinitions.DefineObjectArrayVariable<Effect>("storedEffects");

graph.VariableDefinitions.DefineObjectArrayProperty(
    "effects",
    new EffectArrayVariableResolver("storedEffects"));

var effectNode = new EffectNode();
effectNode.BindInput(EffectNode.EffectInput, "effects");
effectNode.BindInput(EffectNode.TargetInput, "target");
```

## See Also

- [Resolvers Overview](README.md)
- [EffectVariableResolver](effect-variable-resolver.md)
- [EffectArrayFromDataResolver](effect-array-from-data-resolver.md)
- [EffectNode](../nodes/state/effect-node.md)
