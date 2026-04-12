# Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Each resolver implements `IPropertyResolver` and returns a `Variant128` given a `GraphContext`.

For an overview of the Statescript system, see the [Statescript overview](../README.md). For how resolvers fit into the broader data flow, see [Variables and Data](../variables.md). For creating your own resolvers, see [Custom Resolvers](../custom-resolvers.md).

## Built-in Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ArrayVariableResolver](array-resolver.md) | *(configured)* | Stores a mutable array of values with indexed access. |
| [AttributeResolver](attribute-resolver.md) | `int` | Reads the current value of an entity attribute. |
| [ComparisonResolver](comparison-resolver.md) | `bool` | Compares two values using a comparison operation. |
| [MagnitudeResolver](magnitude-resolver.md) | `float` | Reads the magnitude from the ability activation context. |
| [SharedVariableResolver](shared-variable-resolver.md) | *(configured)* | Reads a shared variable from the entity. |
| [TagResolver](tag-resolver.md) | `bool` | Checks whether the owner entity has a specific tag. |
| [VariableResolver](variable-resolver.md) | *(configured)* | Reads a graph variable by name. |
| [VariantResolver](variant-resolver.md) | *(configured)* | Holds a fixed constant value. |

## Math Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbsResolver](abs-resolver.md) | *(promoted)* | Computes the absolute value of a signed numeric value. |
| [AddResolver](add-resolver.md) | *(promoted)* | Adds two numeric or vector values. |
| [CeilResolver](ceil-resolver.md) | *(same)* | Rounds up to the smallest integer greater than or equal to the operand. |
| [ClampResolver](clamp-resolver.md) | *(promoted)* | Clamps a numeric value between a minimum and maximum bound. |
| [DivideResolver](divide-resolver.md) | *(promoted)* | Divides two numeric or vector values. |
| [FloorResolver](floor-resolver.md) | *(same)* | Rounds down to the largest integer less than or equal to the operand. |
| [LerpResolver](lerp-resolver.md) | `float`/`double`/vector/quaternion | Linearly interpolates between two values (scalar, vector, or quaternion). |
| [MaxResolver](max-resolver.md) | *(promoted)* | Returns the larger of two numeric values. |
| [MinResolver](min-resolver.md) | *(promoted)* | Returns the smaller of two numeric values. |
| [ModuloResolver](modulo-resolver.md) | *(promoted)* | Computes the remainder of dividing two numeric values. |
| [MultiplyResolver](multiply-resolver.md) | *(promoted)* | Multiplies two numeric or vector values. |
| [NegateResolver](negate-resolver.md) | *(promoted)* | Negates a numeric or vector value. |
| [PowResolver](pow-resolver.md) | `float`/`double` | Raises a value to a specified power. |
| [RoundResolver](round-resolver.md) | *(same)* | Rounds to the nearest integer using banker's rounding. |
| [SqrtResolver](sqrt-resolver.md) | `float`/`double` | Computes the square root of a numeric value. |
| [SubtractResolver](subtract-resolver.md) | *(promoted)* | Subtracts two numeric or vector values. |
