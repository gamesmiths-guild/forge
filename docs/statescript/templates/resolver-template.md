# {ResolverName}

> **Type:** `{Namespace}.{ClassName}`
> **Output Type:** `{OutputType}` (e.g., `bool`, `int`, `float`, `double`, `Vector3`)

{Brief one-paragraph description of what this resolver does, when to use it, and what it produces.}

## Constructor

```csharp
new {ClassName}({parameters})
```

| Parameter | Type | Description |
|-----------|------|-------------|
| {paramName} | `{ParamType}` | {Description of the parameter.} |

## Behavior

{Describe the resolution logic:}

- {What data it reads (graph variables, activation context, entity attributes, etc.)}
- {How it computes the result.}
- {What it returns as a fallback when data is unavailable.}

## Usage

```csharp
// {Example showing how to create and use this resolver}
```

## Composition

{Show how this resolver composes with other resolvers. For resolvers that accept `IPropertyResolver` operands, show nesting. For leaf resolvers (constants, variable reads), show them being used as operands in math or comparison resolvers.}

```csharp
// {Example showing composition}
```

## See Also

- [{Related resolver or node}]({relative-link})
