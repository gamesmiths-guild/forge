// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an object-backed array by evaluating a nested object projection resolver for each element of a nested
/// source array, like a LINQ <c>Select</c> producing references. The projection is evaluated once per element with
/// the current element published on the element stack, so it can read the element through
/// <see cref="ElementValueResolver"/> (value-typed sources) or
/// <see cref="ElementResolver{T}"/>/<see cref="ElementEntityResolver"/> (object-backed sources).
/// </summary>
/// <remarks>
/// The source may come from either lane: a value-typed array (<see cref="IArrayPropertyResolver"/>) or an object-backed
/// array (<see cref="IObjectArrayResolver"/>).
/// </remarks>
/// <typeparam name="TResult">The element type produced by the projection.</typeparam>
public class SelectObjectResolver<TResult> : ObjectArrayResolver<TResult>
{
	private readonly IArrayPropertyResolver? _valueSource;

	private readonly IObjectArrayResolver? _objectSource;

	private readonly IObjectResolver<TResult> _projection;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectObjectResolver{TResult}"/> class over a value-typed source
	/// array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="projection">The resolver evaluated per element to produce the projected value.</param>
	public SelectObjectResolver(IArrayPropertyResolver source, IObjectResolver<TResult> projection)
		: this(source, null, projection)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectObjectResolver{TResult}"/> class over an object-backed
	/// source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="projection">The resolver evaluated per element to produce the projected value.</param>
	public SelectObjectResolver(IObjectArrayResolver source, IObjectResolver<TResult> projection)
		: this(null, source, projection)
	{
	}

	private SelectObjectResolver(
		IArrayPropertyResolver? valueSource,
		IObjectArrayResolver? objectSource,
		IObjectResolver<TResult> projection)
	{
		_valueSource = valueSource;
		_objectSource = objectSource;
		_projection = projection;
	}

	/// <inheritdoc/>
	public override TResult[] ResolveArray(GraphContext graphContext)
	{
		var sequence = ElementSequence.Resolve(graphContext, _valueSource, _objectSource);
		var values = new TResult[sequence.Length];

		for (int i = 0; i < sequence.Length; i++)
		{
			values[i] = (TResult)ElementLambda.Evaluate(graphContext, _projection, sequence.GetFrame(i))!;
		}

		return values;
	}
}
