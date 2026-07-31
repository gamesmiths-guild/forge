# {ComponentName}

> **Type:** `{Namespace}.{ClassName}`
> **State:** {Stateless — shared across every application | Stateful — returns a new instance from `CreateInstance`}
> **Applies to:** {Instant effects only | Duration effects only | Any effect}

{Brief one-paragraph description of what this component does, when to reach for it, and what it changes on the target.}

## Constructor

```csharp
new {ClassName}({parameters})
```

| Parameter | Type | Description |
|-----------|------|-------------|
| {paramName} | `{ParamType}` | {Description of the parameter. Note the default and what it means when omitted.} |

> Remove the Constructor table if the component takes no parameters.

## Lifecycle Hooks

| Hook | What this component does |
|------|--------------------------|
| {`OnActiveEffectAdded`} | {What it does in this hook.} |

> List only the hooks the component actually overrides, in the order they fire. See [Component Lifecycle Methods](../effects/components/README.md#component-lifecycle-methods) for what each hook guarantees.

## Behavior

{Describe the full behavior:}

- {What it does on application.}
- {How it reacts to inhibition, stacking, and level changes — or state explicitly that it ignores them.}
- {What it undoes on removal, and whether a stack removal counts.}

## Validation

{Describe any `EffectData.ValidateData` assertions that reject a misconfigured effect using this component, and what the misconfiguration would otherwise do at runtime.}

> Remove this section if the component has no validation rules.

## Usage

```csharp
// {Example showing a realistic effect built with this component}
```

## Key Points

- {The non-obvious constraints and gotchas a reader needs before using it.}
- {Where it differs from a similar component, if one exists.}

## See Also

- [Effect Components Overview](README.md)
- [{Related component}]({relative-link})
