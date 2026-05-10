# Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Each resolver implements `IPropertyResolver` and returns a `Variant128` given a `GraphContext`.

For an overview of the Statescript system, see the [Statescript overview](../README.md). For how resolvers fit into the broader data flow, see [Variables and Data](../variables.md). For creating your own resolvers, see [Custom Resolvers](../custom-resolvers.md).

---

## Built-in Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ActivationDataResolver](activation-data-resolver.md) | *(configured)* | Reads a field or property from typed activation data. |
| [ArrayVariableResolver](array-resolver.md) | *(configured)* | Stores a mutable array of values with indexed access. |
| [AttributeResolver](attribute-resolver.md) | `int` | Reads a selected value from an entity attribute. |
| [MagnitudeResolver](magnitude-resolver.md) | `float` | Reads the magnitude from the ability activation context. |
| [SharedVariableResolver](shared-variable-resolver.md) | *(configured)* | Reads a shared variable from the entity. |
| [TagResolver](tag-resolver.md) | `bool` | Checks whether the owner entity has a specific tag. |
| [VariableResolver](variable-resolver.md) | *(configured)* | Reads a graph variable by name. |
| [VariantResolver](variant-resolver.md) | *(configured)* | Holds a fixed constant value. |

---

## Boolean Expressions

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AndResolver](and-resolver.md) | `bool` | Returns `true` only when both boolean operands are `true`. |
| [ComparisonResolver](comparison-resolver.md) | `bool` | Compares two values using a comparison operation. |
| [NotResolver](not-resolver.md) | `bool` | Returns the logical inverse of a boolean operand. |
| [OrResolver](or-resolver.md) | `bool` | Returns `true` when either boolean operand is `true`. |
| [XorResolver](xor-resolver.md) | `bool` | Returns `true` when exactly one boolean operand is `true`. |

---

## Math

### Scalar Math Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ACosHResolver](acosh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic cosine. |
| [ACosResolver](acos-resolver.md) | `float`/`double` | Computes the arc cosine (inverse cosine), returning angle in radians. |
| [ASinHResolver](asinh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic sine. |
| [ASinResolver](asin-resolver.md) | `float`/`double` | Computes the arc sine (inverse sine), returning angle in radians. |
| [ATan2Resolver](atan2-resolver.md) | `float`/`double` | Computes the angle from two coordinates using `ATan2(y, x)`. |
| [ATanHResolver](atanh-resolver.md) | `float`/`double` | Computes the inverse hyperbolic tangent. |
| [ATanResolver](atan-resolver.md) | `float`/`double` | Computes the arc tangent (inverse tangent), returning angle in radians. |
| [CbrtResolver](cbrt-resolver.md) | `float`/`double` | Computes the cube root. |
| [CopySignResolver](copysign-resolver.md) | `float`/`double` | Returns a value with the magnitude of one operand and the sign of another. |
| [CosHResolver](cosh-resolver.md) | `float`/`double` | Computes the hyperbolic cosine. |
| [CosResolver](cos-resolver.md) | `float`/`double` | Computes the cosine of an angle in radians. |
| [EResolver](e-resolver.md) | `float`/`double` | Returns the mathematical constant `e` (Euler's number). |
| [ExpResolver](exp-resolver.md) | `float`/`double` | Computes `e` raised to a specified power (`e^x`). |
| [Log10Resolver](log10-resolver.md) | `float`/`double` | Computes the base-10 logarithm. |
| [Log2Resolver](log2-resolver.md) | `float`/`double` | Computes the base-2 logarithm. |
| [LogResolver](log-resolver.md) | `float`/`double` | Computes the natural logarithm (base `e`). |
| [PiResolver](pi-resolver.md) | `float`/`double` | Returns the mathematical constant π (pi). |
| [SignResolver](sign-resolver.md) | `int` | Returns -1, 0, or 1 indicating the sign of a numeric value. |
| [SinHResolver](sinh-resolver.md) | `float`/`double` | Computes the hyperbolic sine. |
| [SinResolver](sin-resolver.md) | `float`/`double` | Computes the sine of an angle in radians. |
| [TanHResolver](tanh-resolver.md) | `float`/`double` | Computes the hyperbolic tangent. |
| [TanResolver](tan-resolver.md) | `float`/`double` | Computes the tangent of an angle in radians. |

---

### Generic Math Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbsResolver](abs-resolver.md) | *(promoted or same vector type)* | Computes the absolute value of a signed numeric value or vector components. |
| [AddResolver](add-resolver.md) | *(promoted or same vector type)* | Adds two numeric, vector or quaternion values. |
| [CeilResolver](ceil-resolver.md) | *(same)* | Rounds up to the smallest integer greater than or equal to the operand. |
| [ClampResolver](clamp-resolver.md) | *(promoted or same vector type)* | Clamps a numeric value or vector components between minimum and maximum bounds. |
| [DegToRadResolver](degtorad-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts degrees to radians. |
| [DivideResolver](divide-resolver.md) | *(promoted or same vector type)* | Divides two numeric values, vectors component-wise, or two quaternions. |
| [FloorResolver](floor-resolver.md) | *(same)* | Rounds down to the largest integer less than or equal to the operand. |
| [LerpResolver](lerp-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4`/`Quaternion` | Linearly interpolates between two values (scalar, vector, or quaternion). |
| [MaxResolver](max-resolver.md) | *(promoted or same vector type)* | Returns the larger of two numeric values or the component-wise maximum of two vectors. |
| [MinResolver](min-resolver.md) | *(promoted or same vector type)* | Returns the smaller of two numeric values or the component-wise minimum of two vectors. |
| [ModuloResolver](modulo-resolver.md) | *(promoted)* | Computes the remainder of dividing two numeric values. |
| [MultiplyResolver](multiply-resolver.md) | *(promoted or same vector type)* | Multiplies two numeric, vectors component-wise, or two quaternions. |
| [NegateResolver](negate-resolver.md) | *(promoted)* | Negates a numeric or vector value. |
| [PowResolver](pow-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Raises a value to a specified power. |
| [RadToDegResolver](radtodeg-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts radians to degrees. |
| [RoundResolver](round-resolver.md) | *(same)* | Rounds to a specified number of digits with configurable rounding mode. |
| [SqrtResolver](sqrt-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Computes the square root of a numeric value or component-wise square root of a vector. |
| [SubtractResolver](subtract-resolver.md) | *(promoted or same vector type)* | Subtracts two numeric, vector or quaternion values. |
| [TruncateResolver](truncate-resolver.md) | *(same)* | Removes the fractional part, rounding toward zero. |

---

## Spatial Math

### Vector

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AngleResolver](angle-resolver.md) | `float` | Computes the unsigned angle between two vectors or two quaternions. |
| [ClampMagnitudeResolver](clampmagnitude-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Clamps a vector to a maximum magnitude. |
| [CrossResolver](cross-resolver.md) | `Vector3` | Computes the cross product of two `Vector3` operands. |
| [DistanceResolver](distance-resolver.md) | `float` | Computes the Euclidean distance between two vector operands. |
| [DistanceSquaredResolver](distancesquared-resolver.md) | `float` | Computes the squared Euclidean distance between two vector operands. |
| [DotResolver](dot-resolver.md) | `float` | Computes the dot product of two vectors or two quaternions. |
| [LengthResolver](length-resolver.md) | `float` | Computes the length (magnitude) of a vector or quaternion operand. |
| [LengthSquaredResolver](lengthsquared-resolver.md) | `float` | Computes the squared length of a vector or quaternion operand. |
| [NormalizeResolver](normalize-resolver.md) | `Vector2`/`Vector3`/`Vector4`/`Plane`/`Quaternion` | Computes the normalized form of a vector, plane, or quaternion. |
| [ProjectResolver](project-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Projects one vector onto another. |
| [ReflectResolver](reflect-resolver.md) | `Vector2`/`Vector3` | Reflects a vector off a surface defined by a normal vector. |
| [RejectResolver](reject-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Rejects one vector from another. |
| [ScaleResolver](scale-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Scales a vector by a float scalar value. |
| [SignedAngleResolver](signedangle-resolver.md) | `float` | Computes the signed angle between two vectors. |
| [VectorComponentResolver](vectorcomponent-resolver.md) | `float` | Extracts a single component from a vector. |
| [VectorFromValuesResolver](vectorfromvalues-resolver.md) | `Vector2`/`Vector3`/`Vector4` | Creates a vector from float component resolver values. |

---

### Quaternion

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ConcatenateResolver](concatenate-resolver.md) | `Quaternion` | Concatenates two quaternion rotations. |
| [ConjugateResolver](conjugate-resolver.md) | `Quaternion` | Computes the conjugate of a quaternion. |
| [InverseResolver](inverse-resolver.md) | `Quaternion` | Computes the inverse of a quaternion. |
| [LookAtResolver](lookat-resolver.md) | `Quaternion` | Creates a look rotation from one position to another using an up vector. |
| [QuaternionFromAxisAngleResolver](quaternionfromaxisangle-resolver.md) | `Quaternion` | Creates a quaternion from an axis and angle. |
| [QuaternionFromEulerAnglesResolver](quaternionfromeulerangles-resolver.md) | `Quaternion` | Creates a quaternion from Euler angles using an optional Euler order. |
| [QuaternionFromYawPitchRollResolver](quaternionfromyawpitchroll-resolver.md) | `Quaternion` | Creates a quaternion from yaw, pitch, and roll angles. |
| [RotateTowardsResolver](rotatetowards-resolver.md) | `Quaternion` | Rotates one quaternion toward another by a maximum angular delta. |
| [SlerpResolver](slerp-resolver.md) | `Quaternion` | Spherically interpolates between two quaternion rotations. |

---

### Plane

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [DotCoordinateResolver](dotcoordinate-resolver.md) | `float` | Computes the dot product of a plane and a 3D coordinate. |
| [DotNormalResolver](dotnormal-resolver.md) | `float` | Computes the dot product of a plane normal and a vector. |
| [PlaneDistanceResolver](planedistance-resolver.md) | `float` | Extracts the distance component of a plane. |
| [PlaneFromNormalResolver](planefromnormal-resolver.md) | `Plane` | Creates a plane from a normal vector and distance. |
| [PlaneFromVerticesResolver](planefromvertices-resolver.md) | `Plane` | Creates a plane from three vertices. |
| [PlaneNormalResolver](planenormal-resolver.md) | `Vector3` | Extracts the normal component of a plane. |

---

### Utility

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [EulerAnglesFromQuaternionResolver](euleranglesfromquaternion-resolver.md) | `Vector3` | Extracts Euler angles from a quaternion using an optional Euler order. |
| [MoveTowardsResolver](movetowards-resolver.md) | `float`/`Vector2`/`Vector3`/`Vector4` | Moves a value toward a target by a maximum delta. |
| [TransformResolver](transform-resolver.md) | `Vector2`/`Vector3`/`Vector4`/`Plane` | Transforms a vector or plane by a quaternion rotation. |

---

## Random

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [RandomDirectionResolver](randomdirection-resolver.md) | `Vector2` | Returns a random normalized 2D direction. |
| [RandomInsideCircleResolver](randominsidecircle-resolver.md) | `Vector2` | Returns a random point inside the unit circle. |
| [RandomInsideSphereResolver](randominsidesphere-resolver.md) | `Vector3` | Returns a random point inside the unit sphere. |
| [RandomOnSphereResolver](randomonsphere-resolver.md) | `Vector3` | Returns a random normalized 3D direction on the unit sphere. |
| [RandomResolver](random-resolver.md) | `int`/`float`/`double` | Generates a random value in a range using an `IRandom` provider. |
