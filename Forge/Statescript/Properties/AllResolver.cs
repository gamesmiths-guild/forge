// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether every element of a nested array resolver satisfies a nested boolean
/// predicate resolver (e.g. "are all targets dead?"). The predicate is evaluated once per element with the current
/// element published on the element stack, so it can read the element through the element resolvers.
/// </summary>
/// <remarks>
/// The source may come from either lane: a value-typed array (<see cref="IArrayPropertyResolver"/>) or an object-backed
/// array (<see cref="IObjectArrayResolver"/>). An empty array resolves to <see langword="true"/>, and evaluation stops
/// at the first non-matching element.
/// </remarks>
public class AllResolver : IPropertyResolver
{
	private readonly IArrayPropertyResolver? _valueSource;

	private readonly IObjectArrayResolver? _objectSource;

	private readonly IPropertyResolver _predicate;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="AllResolver"/> class over a value-typed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="predicate">The resolver evaluated per element. Must resolve to <see langword="bool"/>.</param>
	public AllResolver(IArrayPropertyResolver source, IPropertyResolver predicate)
		: this(source, null, predicate)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AllResolver"/> class over an object-backed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="predicate">The resolver evaluated per element. Must resolve to <see langword="bool"/>.</param>
	public AllResolver(IObjectArrayResolver source, IPropertyResolver predicate)
		: this(null, source, predicate)
	{
	}

	private AllResolver(
		IArrayPropertyResolver? valueSource,
		IObjectArrayResolver? objectSource,
		IPropertyResolver predicate)
	{
		_valueSource = valueSource;
		_objectSource = objectSource;
		_predicate = BooleanTypeUtils.ValidateBoolOperand(nameof(AllResolver), nameof(predicate), predicate);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var sequence = ElementSequence.Resolve(graphContext, _valueSource, _objectSource);

		for (int i = 0; i < sequence.Length; i++)
		{
			if (!ElementLambda.EvaluatePredicate(graphContext, _predicate, sequence.GetFrame(i)))
			{
				return new Variant128(false);
			}
		}

		return new Variant128(true);
	}
}
