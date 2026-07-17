// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested array resolver that also appear in a second nested array resolver, preserving
/// their original order.
/// </summary>
/// <remarks>
/// Unlike LINQ's set-based <c>Intersect</c>, duplicates in the source are preserved when they appear in
/// <paramref name="other"/>. Floating-point elements are compared exactly.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="other">The resolver providing the elements to keep. Must share the source's element type.</param>
public class IntersectResolver(IArrayPropertyResolver source, IArrayPropertyResolver other) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IArrayPropertyResolver _other = other;

	/// <inheritdoc/>
	public Type ElementType { get; } = ArrayResolverUtils.ValidateMatchingElementTypes(
		nameof(IntersectResolver),
		source.ElementType,
		other.ElementType);

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return values;
		}

		Variant128[] keptValues = _other.ResolveArray(graphContext);

		if (keptValues.Length == 0)
		{
			return [];
		}

		var result = new List<Variant128>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			if (ContainsValue(keptValues, values[i]))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}

	private bool ContainsValue(Variant128[] values, Variant128 value)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (VariantEquality.AreEqual(values[i], value, ElementType))
			{
				return true;
			}
		}

		return false;
	}
}
