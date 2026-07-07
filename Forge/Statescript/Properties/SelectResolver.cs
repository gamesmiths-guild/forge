// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array by evaluating a nested projection resolver for each element of a nested source array, like a LINQ
/// <c>Select</c>. The projection is evaluated once per element with the current element published on the element stack,
/// so it can read the element through <see cref="ElementValueResolver"/> (value-typed sources) or
/// <see cref="ElementResolver{T}"/>/<see cref="ElementEntityResolver"/> (object-backed sources).
/// </summary>
/// <remarks>
/// The source may come from either lane: a value-typed array (<see cref="IArrayPropertyResolver"/>) or an
/// object-backed array (<see cref="IObjectArrayResolver"/>), enabling projections such as "the health of each entity
/// in the array". The resulting element type is the projection's value type.
/// </remarks>
public class SelectResolver : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver? _valueSource;

	private readonly IObjectArrayResolver? _objectSource;

	private readonly IPropertyResolver _projection;

	/// <inheritdoc/>
	public Type ElementType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectResolver"/> class over a value-typed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="projection">The resolver evaluated per element to produce the projected value.</param>
	public SelectResolver(IArrayPropertyResolver source, IPropertyResolver projection)
		: this(source, null, projection)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectResolver"/> class over an object-backed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="projection">The resolver evaluated per element to produce the projected value.</param>
	public SelectResolver(IObjectArrayResolver source, IPropertyResolver projection)
		: this(null, source, projection)
	{
	}

	private SelectResolver(
		IArrayPropertyResolver? valueSource,
		IObjectArrayResolver? objectSource,
		IPropertyResolver projection)
	{
		_valueSource = valueSource;
		_objectSource = objectSource;
		_projection = projection;
		ElementType = projection.ValueType;
	}

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		var sequence = ElementSequence.Resolve(graphContext, _valueSource, _objectSource);
		var values = new Variant128[sequence.Length];

		for (int i = 0; i < sequence.Length; i++)
		{
			values[i] = ElementLambda.Evaluate(graphContext, _projection, sequence.GetFrame(i));
		}

		return values;
	}
}
