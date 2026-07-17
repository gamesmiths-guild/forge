# Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Scalar resolvers implement `IPropertyResolver`, while array resolvers use `IArrayPropertyResolver` or typed reference-array helpers.

For an overview of the Statescript system, see the [Statescript overview](../README.md). For how resolvers fit into the broader data flow, see [Variables and Data](../variables.md). For creating your own resolvers, see [Custom Resolvers](../custom-resolvers.md).

---

## Built-in Resolvers

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityActivationDataResolver](ability-activation-data-resolver.md) | *(configured)* | Reads a field or property from the current ability activation data. |
| [AbilityLevelResolver](ability-level-resolver.md) | `int` | Reads the current ability level from the activation context. |
| [AbilityMagnitudeResolver](ability-magnitude-resolver.md) | `float` | Reads the current ability activation magnitude. |
| [ArrayResolver](array-resolver.md) | *(configured array)* | Builds an array by evaluating nested resolvers for each element. |
| [ArrayVariableResolver](array-variable-resolver.md) | *(configured array)* | Reads an array variable from graph or shared scope. |
| [AttributeResolver](attribute-resolver.md) | `int` | Reads a selected value from an entity attribute. |
| [TagQueryResolver](tag-query-resolver.md) | `bool` | Evaluates a tag query against a selected entity's tags. |
| [VariableResolver](variable-resolver.md) | *(configured)* | Reads a graph or shared variable by name. |
| [VariantResolver](variant-resolver.md) | *(configured)* | Holds a fixed constant value. |

---

## Effect Resolvers

These resolvers provide effect instances and application context to nodes such as `ApplyEffectNode` and `EffectNode`, which take `Effect` instances. Author them with `EffectFromDataResolver` (or `EffectVariableResolver` to reuse a stored instance).

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityOwnershipResolver](ability-ownership-resolver.md) | `EffectOwnership` | Reads the current ability owner/source pair as an effect ownership value. |
| [ActiveEffectDataResolver](active-effect-data-resolver.md) | `double`/`int`/`bool` | Reads a selected runtime value (remaining duration, stacks, level, ...) from an active effect handle. |
| [ActiveEffectEffectResolver](active-effect-effect-resolver.md) | `Effect?` | Reads the live `Effect` instance behind an active effect handle. |
| [ActiveEffectTargetResolver](active-effect-target-resolver.md) | `IForgeEntity?` | Reads the entity an active effect is applied to, from its handle. |
| [EffectArrayFromDataResolver](effect-array-from-data-resolver.md) | `Effect[]` | Builds an array of `Effect` instances sharing the same level, ownership, and SetByCaller magnitudes. |
| [EffectArrayVariableResolver](effect-array-variable-resolver.md) | `Effect[]` | Reads a stored `Effect` instance array from graph or shared scope. |
| [EffectContextDataResolver](effect-context-data-resolver.md) | `EffectApplicationContext` | Produces custom application context data for an effect via an `IEffectContextDataProvider`. |
| [EffectFromDataResolver](effect-from-data-resolver.md) | `Effect` | Builds an `Effect` instance from an `EffectData` value plus optional level, ownership, and SetByCaller magnitudes. |
| [EffectInfoResolver](effect-info-resolver.md) | `int` | Aggregates stack/instance/level info over the active applications of an effect on an entity. |
| [EffectVariableResolver](effect-variable-resolver.md) | `Effect?` | Reads a stored `Effect` instance from graph or shared scope for reuse. |
| [OwnershipResolver](ownership-resolver.md) | `EffectOwnership` | Composes an effect ownership value from nested entity resolvers. |
| [QueryActiveEffectsResolver](query-active-effects-resolver.md) | `ActiveEffectHandle[]` | Queries the active effect handles on an entity, optionally filtered by `EffectData`. |
| [SetByCallerMagnitudeResolver](set-by-caller-magnitude-resolver.md) | `float` | Reads the SetByCaller magnitude stored on an `Effect` for an identifier tag. |

---

## Ability Resolvers

These resolvers read from the ability driving the current graph (through the activation context) or, when given an `IObjectResolver<AbilityHandle>`, from another ability. Look up other abilities with `GetAbilityHandleResolver`.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityCooldownResolver](ability-cooldown-resolver.md) | `float` | Reads a cooldown value (remaining time, total time, or remaining fraction) from an ability. |
| [AbilityCostResolver](ability-cost-resolver.md) | `int` | Reads the evaluated cost of an ability for a specific attribute. |
| [AbilityStateResolver](ability-state-resolver.md) | `bool` | Reads a state flag (is active, is inhibited, is valid) from an ability. |
| [CanActivateAbilityResolver](can-activate-ability-resolver.md) | `bool` | Checks whether an ability can currently activate (cooldowns, costs, tag requirements). |
| [GetAbilityHandleResolver](get-ability-handle-resolver.md) | `AbilityHandle` | Looks up the handle of a granted ability on an entity by its `AbilityData`. |

---

## Cue Resolvers

These resolvers author the optional inputs of the cue nodes (`ExecuteCueNode`, `UpdateCueNode`, `CueNode`).

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [CueCustomParametersResolver](cue-custom-parameters-resolver.md) | `Dictionary<StringKey, object>` | Produces the `CueParameters.CustomParameters` bag for a cue via an `ICueCustomParametersProvider`. |

---

## Event Resolvers

These resolvers author the optional payload of the event nodes (`RaiseEventNode`, `EventListenerNode`) via an `IEventPayloadProvider`.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [EventPayloadOutputResolver](event-payload-resolver.md#listener-side-eventpayloadoutputresolver) | `EventPayloadWriter` | Decomposes a received event payload into graph variables for `EventListenerNode` via an `IEventPayloadProvider`. |
| [EventPayloadResolver](event-payload-resolver.md#raise-side-eventpayloadresolver) | `EventPayloadRaiser` | Builds and raises a typed event payload for `RaiseEventNode` via an `IEventPayloadProvider`. |

---

## Entity Resolvers

Entity resolvers are typed object-backed resolvers used by APIs such as `AttributeResolver` and `TagQueryResolver`. They do
not produce `Variant128` values directly, so they are configured as nested inputs to other resolvers rather than as
regular node-bindable properties.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityOwnerResolver](ability-owner-resolver.md) | `IForgeEntity?` | Resolves the current ability owner entity. |
| [AbilitySourceResolver](ability-source-resolver.md) | `IForgeEntity?` | Resolves the current ability source entity. |
| [AbilityTargetResolver](ability-target-resolver.md) | `IForgeEntity?` | Resolves the current ability target entity. |
| [EntityArrayResolver](entity-array-resolver.md) | `IForgeEntity?[]` | Builds an entity reference array by evaluating nested entity resolvers. |
| [EntityArrayVariableResolver](entity-array-variable-resolver.md) | `IForgeEntity?[]` | Reads an entity reference array from graph or shared scope. |
| [EntityVariableResolver](entity-variable-resolver.md) | `IForgeEntity?` | Reads an entity reference from graph or shared object-backed variables. |

---

## Object Utilities

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [IsValidResolver](is-valid-resolver.md) | `bool` | Checks whether an object-backed resolver produces a valid (non-null) value. |
| [ObjectEqualsResolver](object-equals-resolver.md) | `bool` | Checks whether two object-backed resolvers produce the same instance (reference identity). |

---

## Array Operations

LINQ-inspired resolvers for building array pipelines (filter → sort → take, projections, reductions). Most operations ship in two variants that share a doc page: a value-lane resolver for `Variant128` arrays and an object-lane `Object*Resolver<T>` for reference arrays. Entity-flavored helpers (`Entit*Resolver`) implement `IEntityResolver` so their result plugs into `AttributeResolver`, `TagQueryResolver`, and friends.

### Element (Lambda) Resolvers

Operations that take a nested predicate, key selector, or projection evaluate it once per element with the current element published on the graph context. These resolvers are the "lambda parameter", they read the current element back inside that nested resolver.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ElementEntityResolver](element-entity-resolver.md) | `IForgeEntity?` | Reads the iterated entity; composes with entity-aware resolvers for per-element keys. |
| [ElementIndexResolver](element-index-resolver.md) | `int` | Reads the zero-based index of the element currently being iterated. |
| [ElementResolver&lt;T&gt;](element-resolver.md) | `T?` | Reads the object-backed element currently being iterated. |
| [ElementValueResolver](element-value-resolver.md) | *(configured)* | Reads the value-typed element currently being iterated. |

### Element Access

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [ElementAtResolver](element-at-resolver.md) | *(element type)* | Reads the element at a resolved index. Object/entity variants: `ObjectElementAtResolver<T>`, `EntityElementAtResolver`. |
| [FirstResolver](first-resolver.md) | *(element type)* | Reads the first element. Object/entity variants: `ObjectFirstResolver<T>`, `EntityFirstResolver`. |
| [LastResolver](last-resolver.md) | *(element type)* | Reads the last element. Object/entity variants: `ObjectLastResolver<T>`, `EntityLastResolver`. |

### Transformation

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AppendResolver](append-resolver.md) | *(element array)* | Appends nested-resolver elements to the end. Object variant: `ObjectAppendResolver<T>`. |
| [ConcatResolver](concat-resolver.md) | *(element array)* | Concatenates two arrays. Object variant: `ObjectConcatResolver<T>`. |
| [DistinctResolver](distinct-resolver.md) | *(element array)* | De-duplicates, keeping first occurrences. Object variant: `ObjectDistinctResolver<T>`. |
| [ExceptResolver](except-resolver.md) | *(element array)* | Removes the elements found in another array. Object variant: `ObjectExceptResolver<T>`. |
| [IntersectResolver](intersect-resolver.md) | *(element array)* | Keeps the elements also found in another array. Object variant: `ObjectIntersectResolver<T>`. |
| [OrderByResolver](order-by-resolver.md) | *(element array)* | Stable-sorts elements by a nested numeric key selector. Object variant: `ObjectOrderByResolver<T>`. |
| [RemoveAtResolver](remove-at-resolver.md) | *(element array)* | Removes the element at a resolved index. Object variant: `ObjectRemoveAtResolver<T>`. |
| [ReverseResolver](reverse-resolver.md) | *(element array)* | Reverses the element order. Object variant: `ObjectReverseResolver<T>`. |
| [SelectResolver](select-resolver.md) | *(projected array)* | Projects each element through a nested resolver (either source lane). Object-producing variant: `SelectObjectResolver<TResult>`. |
| [ShuffleResolver](shuffle-resolver.md) | *(element array)* | Produces a random permutation using an `IRandom` provider. Object variant: `ObjectShuffleResolver<T>`. |
| [SkipResolver](skip-resolver.md) | *(element array)* | Drops the first N elements. Object variant: `ObjectSkipResolver<T>`. |
| [TakeResolver](take-resolver.md) | *(element array)* | Keeps the first N elements. Object variant: `ObjectTakeResolver<T>`. |
| [WhereResolver](where-resolver.md) | *(element array)* | Keeps the elements matching a nested boolean predicate. Object variant: `ObjectWhereResolver<T>`. |

### Reductions and Aggregation

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AllResolver](all-resolver.md) | `bool` | Checks whether every element matches a nested predicate (either source lane). |
| [AnyResolver](any-resolver.md) | `bool` | Checks whether any element exists or matches a nested predicate (either source lane). |
| [AverageResolver](average-resolver.md) | `double`/`float`/`decimal` | Computes the arithmetic mean of a numeric array. |
| [ContainsResolver](contains-resolver.md) | `bool` | Checks whether the array contains a resolved value. Object variant: `ObjectContainsResolver`. |
| [CountResolver](count-resolver.md) | `int` | Counts elements, optionally only those matching a nested predicate (either source lane). |
| [IndexOfResolver](index-of-resolver.md) | `int` | Finds the index of the first occurrence of a resolved value, or -1. Object variant: `ObjectIndexOfResolver`. |
| [MaxElementResolver](max-element-resolver.md) | *(element type)* | Returns the largest element of a numeric array. |
| [MinElementResolver](min-element-resolver.md) | *(element type)* | Returns the smallest element of a numeric array. |
| [RandomElementResolver](random-element-resolver.md) | *(element type)* | Picks a random element using an `IRandom` provider. Object variant: `ObjectRandomElementResolver<T>`. |
| [SumResolver](sum-resolver.md) | *(promoted numeric)* | Adds up all elements of a numeric array. |

---

## Boolean Expressions

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AndResolver](and-resolver.md) | `bool` | Returns `true` only when both boolean operands are `true`. |
| [ApproximatelyResolver](approximately-resolver.md) | `bool` | Returns `true` when two numeric values are equal within a tolerance. |
| [ComparisonResolver](comparison-resolver.md) | `bool` | Compares two values using a comparison operation. |
| [ConditionalResolver](conditional-resolver.md) | *(matches branches)* | Selects one of two value-lane branches based on a boolean condition (ternary select). |
| [ConditionalObjectResolver](conditional-object-resolver.md) | *(matches branches)* | Selects one of two object-lane branches based on a boolean condition (e.g. picking an entity). |
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
| [CurveSampleResolver](curve-sample-resolver.md) | `float` | Samples an `ICurve` at a resolved position (engine curve assets plug in via `ICurve`). |
| [DegToRadResolver](degtorad-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts degrees to radians. |
| [DeltaAngleResolver](delta-angle-resolver.md) | `float` | Computes the shortest signed angle difference between two angles in radians. |
| [DivideResolver](divide-resolver.md) | *(promoted or same vector type)* | Divides two numeric values, vectors component-wise, or two quaternions. |
| [FloorResolver](floor-resolver.md) | *(same)* | Rounds down to the largest integer less than or equal to the operand. |
| [InverseLerpResolver](inverse-lerp-resolver.md) | `float`/`double` | Computes the normalized position of a value within a range (inverse of Lerp), clamped to 0-1. |
| [LerpResolver](lerp-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4`/`Quaternion` | Linearly interpolates between two values (scalar, vector, or quaternion). |
| [MaxResolver](max-resolver.md) | *(promoted or same vector type)* | Returns the larger of two numeric values or the component-wise maximum of two vectors. |
| [MinResolver](min-resolver.md) | *(promoted or same vector type)* | Returns the smaller of two numeric values or the component-wise minimum of two vectors. |
| [ModuloResolver](modulo-resolver.md) | *(promoted)* | Computes the remainder of dividing two numeric values. |
| [MultiplyResolver](multiply-resolver.md) | *(promoted or same vector type)* | Multiplies two numeric, vectors component-wise, or two quaternions. |
| [NegateResolver](negate-resolver.md) | *(promoted)* | Negates a numeric or vector value. |
| [PingPongResolver](ping-pong-resolver.md) | `float` | Bounces a value back and forth between 0 and a length. |
| [PowResolver](pow-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Raises a value to a specified power. |
| [RadToDegResolver](radtodeg-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Converts radians to degrees. |
| [RemapResolver](remap-resolver.md) | `float`/`double` | Remaps a value from an input range to an output range, optionally clamped. |
| [RoundResolver](round-resolver.md) | *(same)* | Rounds to a specified number of digits with configurable rounding mode. |
| [SmoothStepResolver](smooth-step-resolver.md) | `float` | Computes the smooth Hermite interpolation of a value between two edges (0-1). |
| [SqrtResolver](sqrt-resolver.md) | `float`/`double`/`Vector2`/`Vector3`/`Vector4` | Computes the square root of a numeric value or component-wise square root of a vector. |
| [SubtractResolver](subtract-resolver.md) | *(promoted or same vector type)* | Subtracts two numeric, vector or quaternion values. |
| [TruncateResolver](truncate-resolver.md) | *(same)* | Removes the fractional part, rounding toward zero. |
| [WrapResolver](wrap-resolver.md) | `float` | Wraps a value into a `[min, max)` range. |

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
