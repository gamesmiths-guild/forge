// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested array resolver that satisfy a nested boolean predicate resolver, preserving their
/// original order. The predicate is evaluated once per element with the current element published on the element stack,
/// so it can read the element through <see cref="ElementValueResolver"/> (and its index through
/// <see cref="ElementIndexResolver"/>).
/// </summary>
/// <remarks>
/// To remove matching elements instead, wrap the predicate in a <see cref="NotResolver"/>.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="predicate">The resolver evaluated per element. Must resolve to <see langword="bool"/>.</param>
public class WhereResolver(IArrayPropertyResolver source, IPropertyResolver predicate) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _predicate =
		BooleanTypeUtils.ValidateBoolOperand(nameof(WhereResolver), nameof(predicate), predicate);

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		var result = new List<Variant128>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			var frame = new ElementFrame(values[i], ElementType, i);

			if (ElementLambda.EvaluatePredicate(graphContext, _predicate, in frame))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}
}
