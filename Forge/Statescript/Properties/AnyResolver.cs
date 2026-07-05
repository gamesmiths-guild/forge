// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether a nested array resolver produces any elements, optionally testing
/// them against a nested boolean predicate resolver (e.g. "is any enemy in range?"). The predicate is evaluated once
/// per element with the current element published on the element stack, so it can read the element through the element
/// resolvers.
/// </summary>
/// <remarks>
/// The source may come from either lane: a value-typed array (<see cref="IArrayPropertyResolver"/>) or an
/// object-backed array (<see cref="IObjectArrayResolver"/>). Evaluation stops at the first matching element.
/// </remarks>
public class AnyResolver : IPropertyResolver
{
	private readonly IArrayPropertyResolver? _valueSource;

	private readonly IObjectArrayResolver? _objectSource;

	private readonly IPropertyResolver? _predicate;

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <summary>
	/// Initializes a new instance of the <see cref="AnyResolver"/> class over a value-typed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="predicate">The optional resolver evaluated per element. Must resolve to <see langword="bool"/>.
	/// When omitted, the resolver only checks that the array is not empty.</param>
	public AnyResolver(IArrayPropertyResolver source, IPropertyResolver? predicate = null)
		: this(source, null, predicate)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AnyResolver"/> class over an object-backed source array.
	/// </summary>
	/// <param name="source">The resolver providing the source array.</param>
	/// <param name="predicate">The optional resolver evaluated per element. Must resolve to <see langword="bool"/>.
	/// When omitted, the resolver only checks that the array is not empty.</param>
	public AnyResolver(IObjectArrayResolver source, IPropertyResolver? predicate = null)
		: this(null, source, predicate)
	{
	}

	private AnyResolver(
		IArrayPropertyResolver? valueSource,
		IObjectArrayResolver? objectSource,
		IPropertyResolver? predicate)
	{
		_valueSource = valueSource;
		_objectSource = objectSource;
		_predicate = predicate is null
			? null
			: BooleanTypeUtils.ValidateBoolOperand(nameof(AnyResolver), nameof(predicate), predicate);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		var sequence = ElementSequence.Resolve(graphContext, _valueSource, _objectSource);

		if (_predicate is null)
		{
			return new Variant128(sequence.Length > 0);
		}

		for (int i = 0; i < sequence.Length; i++)
		{
			if (ElementLambda.EvaluatePredicate(graphContext, _predicate, sequence.GetFrame(i)))
			{
				return new Variant128(true);
			}
		}

		return new Variant128(false);
	}
}
